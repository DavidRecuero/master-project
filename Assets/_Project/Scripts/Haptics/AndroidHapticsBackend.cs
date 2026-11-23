using System;
using UnityEngine;

/// <summary>
/// Plays haptics on Android through the native Vibrator (JNI), choosing the best option the device has:
/// 1. Android 8+ (API 26) with amplitude control: the waveform with its own strengths. Preferred, because the
///    game controls how soft each one is (system effects felt harsh on some Samsung phones, like the S9).
/// 2. Android 10+ (API 29) without amplitude control: the system effect of the pattern (tick, click, heavy click),
///    tuned by the maker for that motor; better than a full-power pulse. Only single pulses have one.
/// 3. Android 8+ without amplitude control and no system effect: the waveform as on/off timings (strength by duration).
/// 4. Older (min SDK is 25): the legacy vibrate(ms) / vibrate(pattern) calls.
/// It also follows the phone's "Touch feedback" setting when asked to (Settings > Sound and vibration).
/// Needs android.permission.VIBRATE in the manifest. Only create it on a device: the calls throw elsewhere.
/// </summary>
public class AndroidHapticsBackend : IHapticsBackend, IDisposable
{
    private const int ApiOreo = 26;    // VibrationEffect
    private const int ApiQ = 29;       // Predefined effects
    private const int ApiS = 31;       // VibratorManager
    private const int DefaultAmplitude = -1; // VibrationEffect.DEFAULT_AMPLITUDE

    private readonly bool _respectSystemSetting;
    private readonly bool _log;

    private AndroidJavaObject _vibrator;
    private AndroidJavaClass _effectClass;
    private AndroidJavaObject _contentResolver;
    private int _sdk;
    private bool _hasVibrator;
    private bool _hasAmplitudeControl;
    private bool _systemTouchFeedbackOn = true;
    private bool _loggedSystemOff;

    /// <param name="respectSystemSetting">Don't vibrate when the phone's "Touch feedback" is off.</param>
    /// <param name="log">Writes what is played to the device log (logcat).</param>
    public AndroidHapticsBackend(bool respectSystemSetting = false, bool log = false)
    {
        _respectSystemSetting = respectSystemSetting;
        _log = log;

        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _sdk = version.GetStatic<int>("SDK_INT");

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = GetVibrator(activity);
                _contentResolver = activity.Call<AndroidJavaObject>("getContentResolver");
            }

            _hasVibrator = _vibrator != null && _vibrator.Call<bool>("hasVibrator");
            _hasAmplitudeControl = _hasVibrator && _sdk >= ApiOreo && _vibrator.Call<bool>("hasAmplitudeControl");
            if (_sdk >= ApiOreo) _effectClass = new AndroidJavaClass("android.os.VibrationEffect");

            RefreshSystemSetting();

            if (_log) Debug.Log($"[Haptics] API {_sdk}, vibrator {_hasVibrator}, amplitude control {_hasAmplitudeControl}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Haptics] Vibrator not available: {e.Message}");
            _hasVibrator = false;
        }
    }

    public bool IsSupported => _hasVibrator;

    public int SdkLevel => _sdk;
    public bool HasAmplitudeControl => _hasAmplitudeControl;

    /// <summary>Reads the phone's "Touch feedback" setting again (call it when the app comes back to the front).</summary>
    public void RefreshSystemSetting()
    {
        if (!_respectSystemSetting || _contentResolver == null) return;

        try
        {
            using (var settings = new AndroidJavaClass("android.provider.Settings$System"))
                _systemTouchFeedbackOn = settings.CallStatic<int>("getInt", _contentResolver, "haptic_feedback_enabled", 1) != 0;
        }
        catch (Exception)
        {
            _systemTouchFeedbackOn = true; // Unknown: don't block the game's own setting
        }
    }

    public void Play(HapticPattern pattern)
    {
        if (!_hasVibrator || pattern == null || !pattern.IsValid) return;

        if (_respectSystemSetting && !_systemTouchFeedbackOn)
        {
            if (!_loggedSystemOff)
            {
                Debug.Log("[Haptics] The phone's Touch feedback is off: the game doesn't vibrate.");
                _loggedSystemOff = true;
            }
            return;
        }

        try
        {
            if (_sdk >= ApiOreo) PlayWithEffect(pattern);
            else PlayLegacy(pattern);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Haptics] Vibration failed: {e.Message}");
        }
    }

    public void Cancel()
    {
        if (!_hasVibrator) return;
        try { _vibrator.Call("cancel"); }
        catch (Exception) { }
    }

    public void Dispose()
    {
        _vibrator?.Dispose();
        _effectClass?.Dispose();
        _contentResolver?.Dispose();
        _vibrator = null;
        _effectClass = null;
        _contentResolver = null;
        _hasVibrator = false;
    }

    // ---------- Internals ----------

    private AndroidJavaObject GetVibrator(AndroidJavaObject activity)
    {
        if (_sdk >= ApiS)
        {
            using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
            {
                if (manager != null) return manager.Call<AndroidJavaObject>("getDefaultVibrator");
            }
        }

        return activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }

    private void PlayWithEffect(HapticPattern pattern)
    {
        using (AndroidJavaObject effect = CreateEffect(pattern))
        {
            _vibrator.Call("vibrate", effect);
        }

        if (_log) Debug.Log($"[Haptics] {Describe(pattern)}");
    }

    private AndroidJavaObject CreateEffect(HapticPattern pattern)
    {
        if (UsesPredefinedEffect(pattern))
            return _effectClass.CallStatic<AndroidJavaObject>("createPredefined", pattern.PredefinedEffect);

        if (pattern.IsSinglePulse)
        {
            int amplitude = _hasAmplitudeControl ? pattern.Amplitudes[0] : DefaultAmplitude;
            return _effectClass.CallStatic<AndroidJavaObject>("createOneShot", pattern.Timings[0], amplitude);
        }

        if (_hasAmplitudeControl)
            return _effectClass.CallStatic<AndroidJavaObject>("createWaveform", pattern.Timings, pattern.Amplitudes, -1);

        return _effectClass.CallStatic<AndroidJavaObject>("createWaveform", pattern.ToOnOffTimings(), -1);
    }

    /// <summary>The system effect only where the game can't set the strength itself.</summary>
    private bool UsesPredefinedEffect(HapticPattern pattern)
        => pattern.HasPredefinedEffect && _sdk >= ApiQ && !_hasAmplitudeControl;

    private void PlayLegacy(HapticPattern pattern)
    {
        if (pattern.IsSinglePulse) _vibrator.Call("vibrate", pattern.Timings[0]);
        else _vibrator.Call("vibrate", pattern.ToOnOffTimings(), -1);
    }

    private string Describe(HapticPattern pattern)
    {
        if (UsesPredefinedEffect(pattern)) return $"predefined effect {pattern.PredefinedEffect}";
        return $"{pattern.TotalMilliseconds} ms, {pattern.Timings.Length} segments, amplitude {(_hasAmplitudeControl ? "on" : "off")}";
    }
}
