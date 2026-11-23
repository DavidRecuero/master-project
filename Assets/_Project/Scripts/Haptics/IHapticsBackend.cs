/// <summary>
/// The device side of haptics: plays a pattern on the hardware. HapticsService decides whether and what
/// to play; the backend only knows how. Android has a native backend; everywhere else (Editor, tests)
/// a backend that does nothing (or logs) is used.
/// </summary>
public interface IHapticsBackend
{
    /// <summary>The device has a vibrator the game can use.</summary>
    bool IsSupported { get; }

    void Play(HapticPattern pattern);

    /// <summary>Stops whatever is vibrating now.</summary>
    void Cancel();
}

/// <summary>Backend for platforms without haptics. In the Editor it can log, to check what would vibrate.</summary>
public class NullHapticsBackend : IHapticsBackend
{
    private readonly bool _supported;
    private readonly bool _log;

    /// <param name="supported">What IsSupported reports. True in the Editor, so the Settings row can be seen and tested.</param>
    /// <param name="log">Writes every play to the console.</param>
    public NullHapticsBackend(bool supported = false, bool log = false)
    {
        _supported = supported;
        _log = log;
    }

    public bool IsSupported => _supported;

    public void Play(HapticPattern pattern)
    {
        if (_log && pattern != null)
            UnityEngine.Debug.Log($"[Haptics] Vibrate {pattern.TotalMilliseconds} ms ({pattern.Timings.Length} segments)");
    }

    public void Cancel() { }
}
