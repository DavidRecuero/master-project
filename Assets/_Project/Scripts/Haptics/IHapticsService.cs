/// <summary>
/// What the game asks of the haptics system. Gameplay and UI code talk to this interface only,
/// so tests can swap in a fake and nothing depends on the Android vibrator directly.
/// </summary>
public interface IHapticsService
{
    /// <summary>The player's Settings choice. Saved on the device.</summary>
    bool Enabled { get; set; }

    /// <summary>False on devices without a vibrator: the Settings row hides.</summary>
    bool IsSupported { get; }

    /// <summary>Vibrates with the preset of that type, if haptics are on and supported.</summary>
    void Play(HapticType type);
}
