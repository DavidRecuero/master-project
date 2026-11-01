using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Switch look for a Toggle: the knob slides from one end of the track to the other (with a small
/// overshoot and a squash-and-stretch while it travels) and the "on" track fades in under it.
/// Only a player tap animates; SetIsOnWithoutNotify (popup opening with the saved state) snaps.
/// Runs on unscaled time, so it also works in popups opened during a paused game.
/// Leave the Toggle's own "Graphic" empty: this component drives the on track.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class SwitchToggleView : MonoBehaviour
{
    [Tooltip("Track drawn on top of the off track (green); fades in when the switch is on")]
    [SerializeField] private Graphic onTrack;
    [Tooltip("The knob that slides")]
    [SerializeField] private RectTransform knob;

    [Header("Positions (canvas units, from the toggle centre)")]
    public float offX = -36f;
    public float onX = 36f;

    [Header("Animation")]
    public float duration = 0.28f;
    [Tooltip("How far the knob goes past its end before settling (0 = none, 1 = classic back ease)")]
    public float overshoot = 0.9f;
    [Tooltip("Extra knob width in the middle of the slide (squash and stretch)")]
    public float stretch = 0.18f;

    private Toggle _toggle;
    private Coroutine _routine;
    private bool _shownOn;

    /// <summary>True while the knob is moving.</summary>
    public bool IsAnimating => _routine != null;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        if (_toggle != null) _toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnEnable()
    {
        Snap(_toggle.isOn);
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        Snap(_toggle.isOn);
    }

    private void LateUpdate()
    {
        // SetIsOnWithoutNotify fires no event: catch it here and jump without animating
        if (_routine == null && _toggle.isOn != _shownOn) Snap(_toggle.isOn);
    }

    private void OnValueChanged(bool isOn)
    {
        if (!isActiveAndEnabled)
        {
            Snap(isOn);
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Slide(isOn));
    }

    /// <summary>Puts the switch on a state at once.</summary>
    public void Snap(bool isOn)
    {
        _shownOn = isOn;
        Apply(isOn ? onX : offX, isOn ? 1f : 0f, 0f);
    }

    private IEnumerator Slide(bool isOn)
    {
        _shownOn = isOn;
        float fromX = knob != null ? knob.anchoredPosition.x : (isOn ? offX : onX);
        float toX = isOn ? onX : offX;
        float fromAlpha = onTrack != null ? onTrack.color.a : (isOn ? 0f : 1f);
        float toAlpha = isOn ? 1f : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float x = Mathf.LerpUnclamped(fromX, toX, EaseOutBack(p, overshoot));
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 1.6f)));
            Apply(x, alpha, Mathf.Sin(p * Mathf.PI));
            yield return null;
        }

        Apply(toX, toAlpha, 0f);
        _routine = null;
    }

    /// <param name="squash">0 = round knob, 1 = most stretched</param>
    private void Apply(float knobX, float onAlpha, float squash)
    {
        if (knob != null)
        {
            Vector2 pos = knob.anchoredPosition;
            pos.x = knobX;
            knob.anchoredPosition = pos;
            float k = stretch * squash;
            knob.localScale = new Vector3(1f + k, 1f - k * 0.5f, 1f);
        }

        if (onTrack != null)
        {
            Color c = onTrack.color;
            c.a = onAlpha;
            onTrack.color = c;
        }
    }

    /// <summary>Ease out that goes a little past 1 and settles. overshoot 0 = plain cubic ease out.</summary>
    public static float EaseOutBack(float t, float overshoot)
    {
        float c3 = overshoot + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + overshoot * u * u;
    }
}
