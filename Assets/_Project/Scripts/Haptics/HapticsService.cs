using System;
using System.Collections.Generic;

/// <summary>
/// Decides whether a vibration plays and hands its pattern to the backend. Rules:
/// - Nothing plays when the player turned vibration off, or the device has no vibrator.
/// - The same type can't repeat faster than its cooldown (fast taps give separate ticks, not a constant buzz).
/// - On Android a new vibration cuts the one playing, so a weaker one is dropped while a stronger pattern
///   is still going (an item landing must not cut the "level won" pattern).
/// Pure C#: the time comes in as a function, so it is testable in EditMode.
/// </summary>
public class HapticsService : IHapticsService
{
    /// <summary>Default minimum seconds between two plays of the same type.</summary>
    public const float DefaultCooldown = 0.05f;

    private readonly IHapticsBackend _backend;
    private readonly HapticsSettingsStore _settings;
    private readonly Func<float> _now;
    private readonly Func<HapticType, HapticPattern> _patterns;
    private readonly Dictionary<HapticType, float> _lastPlayed = new Dictionary<HapticType, float>();

    private bool _enabled;
    private float _busyUntil = float.NegativeInfinity;
    private int _busyPriority = -1;

    /// <param name="backend">Where the vibration goes.</param>
    /// <param name="settings">Saved on/off choice. Null keeps it in memory only (on).</param>
    /// <param name="now">Current time in seconds (Time.unscaledTime in the game).</param>
    /// <param name="patterns">Pattern for each type. Defaults to HapticPresets.</param>
    public HapticsService(IHapticsBackend backend, HapticsSettingsStore settings = null, Func<float> now = null,
                          Func<HapticType, HapticPattern> patterns = null)
    {
        _backend = backend ?? new NullHapticsBackend();
        _settings = settings;
        _now = now ?? (() => 0f);
        _patterns = patterns ?? HapticPresets.Get;
        _enabled = settings?.Enabled ?? true;
    }

    public bool IsSupported => _backend.IsSupported;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (_settings != null) _settings.Enabled = value;
            if (!value)
            {
                _backend.Cancel();
                _busyUntil = float.NegativeInfinity;
            }
        }
    }

    public void Play(HapticType type)
    {
        if (!_enabled || !_backend.IsSupported || type == HapticType.None) return;

        HapticPattern pattern = _patterns(type);
        if (pattern == null || !pattern.IsValid) return;

        float now = _now();
        int priority = PriorityOf(type);

        if (now < _busyUntil && priority < _busyPriority) return;
        if (_lastPlayed.TryGetValue(type, out float last) && now - last < CooldownOf(type)) return;

        _lastPlayed[type] = now;
        _busyUntil = now + pattern.TotalMilliseconds / 1000f;
        _busyPriority = priority;
        _backend.Play(pattern);
    }

    /// <summary>Higher wins: a pattern can't be cut by a lower one while it plays.</summary>
    public static int PriorityOf(HapticType type)
    {
        switch (type)
        {
            case HapticType.Selection: return 0;
            case HapticType.LightImpact: return 1;
            case HapticType.MediumImpact: return 2;
            case HapticType.HeavyImpact: return 3;
            case HapticType.Warning: return 4;
            case HapticType.Success: return 5;
            case HapticType.Failure: return 5;
            default: return -1;
        }
    }

    /// <summary>Notifications are events, they don't repeat in a burst: a longer cooldown keeps them from stacking.</summary>
    public static float CooldownOf(HapticType type)
    {
        switch (type)
        {
            case HapticType.Success:
            case HapticType.Warning:
            case HapticType.Failure:
                return 0.5f;
            default:
                return DefaultCooldown;
        }
    }
}
