using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual movement of one item: sliding along its pipe, growing on the exit platform, the idle pulse,
/// the arc to the tray and sliding between tray slots, with a bit of squash and stretch.
/// It only moves the drawing: the Item's grid position and the tray contents change at once, so
/// the gameplay never waits for an animation. Added by Item; it is the only writer of the item's
/// position (while moving), scale and rotation.
/// Along the pipe the item follows a route (the cell centres it still has to go through): a tap that comes
/// while it is still sliding adds the next cell to the route, so it keeps moving through the tube and turns
/// the corners, never stopping half way nor cutting across to the new cell.
/// Turned off when the item is disabled or another effect takes over the scale (the match pop):
/// any movement left jumps to its end.
/// </summary>
public class ItemMotion : MonoBehaviour
{
    private enum Path { None, Line, Arc }

    private ItemMotionSettings _settings;

    // ---- Position
    private Path _path;
    private Vector3 _from;
    private Vector3 _to;
    // Line path: the points still to go through, from where the movement started (2 points = a straight line)
    private readonly List<Vector3> _route = new List<Vector3>(4);
    private int _routeNext = 1;    // Index of the route point it is heading to
    private bool _chain;           // A slide along the pipe: a new step extends the route
    private float _moveStart;      // Time.time when it starts moving (after the delay)
    private float _moveDuration;
    private bool _stretchWhileMoving;
    private float _landSquash;
    private float _landDuration;
    private float _landStart = -1f;
    private Action _onArrived;

    // ---- Scale
    private float _baseScale = 1f;
    private float _growFrom;
    private float _growStart = -1f;
    private float _flyScaleFrom;   // Scale the flight starts with (the platform size)
    private bool _pulse;
    private float _pulseSpeed;
    private float _pulseAmount;

    // ---- Tray: once it flies to the tray its size is fixed (no growth, no pulse, no other size)
    private bool _sizeLocked;

    public ItemMotionSettings Settings => _settings != null ? _settings : ItemMotionSettings.Defaults;

    /// <summary>True while it slides or flies (the landing squash doesn't count).</summary>
    public bool IsMoving => _path != Path.None;

    /// <summary>True while it grows to a new size.</summary>
    public bool IsGrowing => _growStart >= 0f;

    /// <summary>Resting size, before the pulse and the squash.</summary>
    public float BaseScale => _baseScale;

    /// <summary>Size the item has on screen right now, without the squash (where a new movement starts from).</summary>
    public float CurrentScale => CurrentBase(Time.time);

    public void Configure(ItemMotionSettings settings)
    {
        _settings = settings;
    }

    // ---------- Commands ----------

    /// <summary>New resting size, at once (no growth).</summary>
    public void SetBaseScale(float scale)
    {
        if (_sizeLocked)
        {
            WarnLocked(scale);
            return;
        }

        _baseScale = scale;
        _growStart = -1f;
        if (!IsMoving || _path != Path.Arc) ApplyScale(scale, 1f, 1f);
    }

    /// <summary>Grows (or shrinks) from one size to the resting size, with the overshoot of the settings.</summary>
    public void GrowTo(float from, float to, float delay)
    {
        if (_sizeLocked)
        {
            WarnLocked(to);
            return;
        }

        _growFrom = from;
        _baseScale = to;
        _growStart = Time.time + Mathf.Max(0f, delay);
        if (!CanAnimate) Complete();
    }

    /// <summary>Idle pulse on top of the resting size (the item waiting on the platform).</summary>
    public void SetPulse(bool on, float speed, float amount)
    {
        if (_sizeLocked) on = false;
        _pulse = on;
        _pulseSpeed = speed;
        _pulseAmount = amount;
    }

    /// <summary>Slides in a straight line to the target: one cell along the pipe, or to another tray slot.</summary>
    /// <param name="chain">A step along the pipe. If the item is still on its way from a previous step, the
    /// target is added to its route instead of starting again: it goes on through the cells in order.</param>
    public void SlideTo(Vector3 target, float delay, float duration, bool stretch, float landSquash, float landDuration, bool chain = false)
    {
        if (chain && _chain && _path == Path.Line && CanAnimate)
        {
            ExtendRoute(target, duration);
        }
        else
        {
            StartMove(Path.Line, transform.position, target, delay, duration, null);
            _chain = chain;
        }

        _stretchWhileMoving = stretch;
        _landSquash = landSquash;
        _landDuration = landDuration;
    }

    /// <summary>Slides from a given point (a new item coming in through the inlet).</summary>
    public void SlideFrom(Vector3 start, Vector3 target, float delay, float duration, bool stretch, float landSquash, float landDuration, bool chain = false)
    {
        transform.position = start;
        _path = Path.None;   // a fresh movement from that point, never an extension
        SlideTo(target, delay, duration, stretch, landSquash, landDuration, chain);
    }

    /// <summary>
    /// Flies in an arc to a tray slot. startScale: the size it leaves with; endScale: its tray size, which
    /// stays fixed from now on (no growth, no pulse) until the item goes back to the pool.
    /// onLanded runs once, when it reaches the slot (or at once if it can't animate).
    /// </summary>
    public void FlyTo(Vector3 target, float startScale, float endScale, Action onLanded)
    {
        ItemMotionSettings s = Settings;
        _flyScaleFrom = startScale;
        _baseScale = endScale;
        _growStart = -1f;
        _pulse = false;
        _sizeLocked = true;
        StartMove(Path.Arc, transform.position, target, 0f, s.flyDuration, onLanded);
        _stretchWhileMoving = true;
        _landSquash = s.trayLandSquash;
        _landDuration = s.trayLandDuration;
        if (!CanAnimate) Complete();
    }

    /// <summary>Changes where the current flight or slide ends (the tray reordered while it was on its way).</summary>
    public void Retarget(Vector3 target)
    {
        if (!IsMoving)
        {
            transform.position = target;
            return;
        }

        _to = target;
        if (_path == Path.Line && _route.Count > 0) _route[_route.Count - 1] = target;
    }

    /// <summary>Ends every movement at once: final position, resting size, no squash.</summary>
    public void Complete()
    {
        if (_path != Path.None) transform.position = EndPoint();
        _path = Path.None;
        _growStart = -1f;
        _landStart = -1f;
        transform.localRotation = Quaternion.identity;
        ApplyScale(_baseScale, 1f, 1f);
        InvokeArrived();
    }

    /// <summary>Back to rest, for the pool (no callback: the item is leaving the game).</summary>
    public void ResetMotion()
    {
        _sizeLocked = false;
        _onArrived = null;
        _path = Path.None;
        _growStart = -1f;
        _landStart = -1f;
        _pulse = false;
        transform.localRotation = Quaternion.identity;
    }

    // ---------- Update ----------

    private bool CanAnimate => Application.isPlaying && isActiveAndEnabled;

    private void OnDisable()
    {
        // Hidden by a higher pipe, sent to the pool or the scene is closing: no half movements left behind
        Complete();
    }

    private void LateUpdate()
    {
        float now = Time.time;
        float sx = 1f, sy = 1f, tilt = 0f;

        if (_path != Path.None)
        {
            float k = _moveDuration > 0f ? Mathf.Clamp01((now - _moveStart) / _moveDuration) : 1f;
            if (now < _moveStart) k = 0f;

            if (_path == Path.Line)
            {
                transform.position = RoutePoint(_route, Easing.OutCubic(k), out Vector3 direction, out _routeNext);
                if (_stretchWhileMoving && k > 0f && k < 1f)
                {
                    float s = Mathf.Sin(Mathf.PI * k);
                    StretchAlong(direction, Settings.stepStretch * s, out sx, out sy);
                }
            }
            else
            {
                ItemMotionSettings set = Settings;
                float e = Easing.InOutCubic(k);
                transform.position = ArcPoint(_from, _to, set.flyArcHeight, e);
                float s = Mathf.Sin(Mathf.PI * k);
                sx = 1f - set.flyStretch * 0.8f * s;
                sy = 1f + set.flyStretch * s;
                tilt = -Mathf.Sign(_to.x - _from.x) * set.flyTilt * s;
            }

            if (k >= 1f) Arrive(now);
        }

        if (_landStart >= 0f)
        {
            float l = _landDuration > 0f ? (now - _landStart) / _landDuration : 1f;
            if (l >= 1f) _landStart = -1f;
            else
            {
                float b = LandBounce(l) * _landSquash;
                sx = 1f + b;
                sy = 1f - b * 0.95f;
            }
        }

        transform.localRotation = tilt != 0f ? Quaternion.Euler(0f, 0f, tilt) : Quaternion.identity;
        ApplyScale(CurrentBase(now), sx, sy);
    }

    // ---------- Helpers ----------

    // A tray item was asked for another size: ignored. Logged (with the call stack) to find who asked.
    private void WarnLocked(float requested)
    {
        if (Mathf.Abs(requested - _baseScale) > 0.001f)
            Debug.LogWarning($"[ItemMotion] {name}: size {requested:0.###} ignored, it is in the tray ({_baseScale:0.###}).", this);
    }

    private void StartMove(Path path, Vector3 from, Vector3 to, float delay, float duration, Action onArrived)
    {
        InvokeArrived();   // A movement that is replaced still counts as arrived

        _path = path;
        _from = from;
        _to = to;
        _moveStart = Time.time + Mathf.Max(0f, delay);
        _moveDuration = Mathf.Max(0f, duration);
        _onArrived = onArrived;
        _landStart = -1f;
        _chain = false;

        _route.Clear();
        if (path == Path.Line)
        {
            _route.Add(from);
            _route.Add(to);
        }
        _routeNext = 1;

        if (!CanAnimate) Complete();
    }

    /// <summary>
    /// A new step while still sliding along the pipe: the route becomes "from here, through the cells still
    /// ahead, to the new cell". An item still waiting for its turn in the wave keeps its start time; one
    /// already moving goes on at once. More than one cell to go takes a bit longer, but less than a full
    /// step per cell (Settings.chainCatchUp), so fast taps catch up.
    /// </summary>
    private void ExtendRoute(Vector3 target, float stepDuration)
    {
        float now = Time.time;
        bool waiting = now < _moveStart;
        Vector3 last = _route[_route.Count - 1];

        // What is still ahead: from the point it is heading to (all of it while it waits)
        int next = waiting ? 1 : Mathf.Clamp(_routeNext, 1, _route.Count - 1);
        if (next > 1) _route.RemoveRange(1, next - 1);
        _route[0] = transform.position;
        if ((target - last).sqrMagnitude > 0.000001f) _route.Add(target);
        _routeNext = 1;

        float cell = Mathf.Max(0.0001f, (target - last).magnitude);
        float cells = RouteLength(_route) / cell;

        _to = target;
        _from = transform.position;
        if (!waiting) _moveStart = now;
        _moveDuration = Mathf.Max(0f, stepDuration) * Mathf.Max(0.5f, 1f + Settings.chainCatchUp * (cells - 1f));
        _landStart = -1f;
    }

    private Vector3 EndPoint() => _path == Path.Line && _route.Count > 0 ? _route[_route.Count - 1] : _to;

    private void Arrive(float now)
    {
        transform.position = EndPoint();
        _path = Path.None;
        if (_landSquash > 0f && _landDuration > 0f) _landStart = now;
        InvokeArrived();
    }

    private void InvokeArrived()
    {
        Action callback = _onArrived;
        _onArrived = null;
        callback?.Invoke();
    }

    private float CurrentBase(float now)
    {
        float scale = _baseScale;

        if (_path == Path.Arc)
        {
            // From the platform size to the tray size, a bit bigger at the top of the arc
            float k = _moveDuration > 0f ? Mathf.Clamp01((now - _moveStart) / _moveDuration) : 1f;
            float e = Easing.InOutCubic(k);
            scale = Mathf.Lerp(_flyScaleFrom, _baseScale, e) * (1f + Settings.flyScaleBoost * Mathf.Sin(Mathf.PI * e));
        }
        else if (_growStart >= 0f)
        {
            ItemMotionSettings set = Settings;
            float g = set.growDuration > 0f ? (now - _growStart) / set.growDuration : 1f;
            if (g >= 1f) _growStart = -1f;
            else scale = Mathf.LerpUnclamped(_growFrom, _baseScale, Easing.OutBack(Mathf.Max(0f, g), set.growOvershoot));
        }

        if (_pulse && _path == Path.None && _growStart < 0f)
            scale += Mathf.Sin(now * _pulseSpeed) * _pulseAmount;

        return scale;
    }

    private void ApplyScale(float scale, float sx, float sy)
    {
        transform.localScale = new Vector3(scale * sx, scale * sy, 1f);
    }

    /// <summary>Stretch along the direction of the movement (taller going up/down, wider going sideways).</summary>
    private static void StretchAlong(Vector3 direction, float amount, out float sx, out float sy)
    {
        bool vertical = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);
        float along = 1f + amount, across = 1f - amount * 0.75f;
        sx = vertical ? across : along;
        sy = vertical ? along : across;
    }

    /// <summary>Total length of a route (sum of its straight segments).</summary>
    public static float RouteLength(IReadOnlyList<Vector3> route)
    {
        float length = 0f;
        for (int i = 1; i < route.Count; i++) length += (route[i] - route[i - 1]).magnitude;
        return length;
    }

    /// <summary>
    /// Point at fraction t (0 to 1) of a route, measured by distance: it goes through every point in order, so
    /// it turns the corners of the pipe. direction: the segment it is on. nextIndex: the point it is heading to.
    /// </summary>
    public static Vector3 RoutePoint(IReadOnlyList<Vector3> route, float t, out Vector3 direction, out int nextIndex)
    {
        direction = Vector3.zero;
        nextIndex = Mathf.Max(1, route.Count - 1);
        if (route.Count == 0) return Vector3.zero;
        if (route.Count == 1) return route[0];

        float total = RouteLength(route);
        direction = route[route.Count - 1] - route[route.Count - 2];
        if (total <= 0f || t >= 1f) return route[route.Count - 1];

        float target = Mathf.Max(0f, t) * total;
        for (int i = 1; i < route.Count; i++)
        {
            float segment = (route[i] - route[i - 1]).magnitude;
            if (target <= segment || i == route.Count - 1)
            {
                direction = route[i] - route[i - 1];
                nextIndex = i;
                return segment > 0f ? Vector3.LerpUnclamped(route[i - 1], route[i], target / segment) : route[i];
            }
            target -= segment;
        }
        return route[route.Count - 1];
    }

    public static Vector3 RoutePoint(IReadOnlyList<Vector3> route, float t) => RoutePoint(route, t, out _, out _);

    /// <summary>Point of a parabola from 'from' to 'to' that rises 'height' above the straight line in the middle.</summary>
    public static Vector3 ArcPoint(Vector3 from, Vector3 to, float height, float t)
    {
        Vector3 p = Vector3.LerpUnclamped(from, to, t);
        p.y += height * 4f * t * (1f - t);
        return p;
    }

    /// <summary>0 → up → 0 with a little rebound: the shape of the squash after landing (t from 0 to 1).</summary>
    public static float LandBounce(float t)
    {
        if (t <= 0f || t >= 1f) return 0f;
        return Mathf.Sin(Mathf.PI * t) * (1f - t);
    }
}

/// <summary>Easing curves (t from 0 to 1).</summary>
public static class Easing
{
    public static float OutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    public static float InOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    /// <summary>Goes past 1 and comes back. overshoot 0 = no bounce (plain ease out).</summary>
    public static float OutBack(float t, float overshoot = 1.70158f)
    {
        t = Mathf.Clamp01(t);
        float c3 = overshoot + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + overshoot * u * u;
    }
}
