using System.Collections.Generic;

/// <summary>Records what the code under test asked of the haptics system.</summary>
public class FakeHapticsService : IHapticsService
{
    public readonly List<HapticType> Played = new List<HapticType>();

    public bool Enabled { get; set; } = true;
    public bool IsSupported { get; set; } = true;

    public void Play(HapticType type) => Played.Add(type);

    public int Count(HapticType type) => Played.FindAll(t => t == type).Count;
    public HapticType Last => Played.Count > 0 ? Played[Played.Count - 1] : HapticType.None;
}

/// <summary>Records the patterns that would reach the vibrator.</summary>
public class FakeHapticsBackend : IHapticsBackend
{
    public readonly List<HapticPattern> Played = new List<HapticPattern>();
    public int CancelCalls { get; private set; }

    public bool IsSupported { get; set; } = true;

    public void Play(HapticPattern pattern) => Played.Add(pattern);
    public void Cancel() => CancelCalls++;
}
