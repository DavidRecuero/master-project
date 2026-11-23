/// <summary>
/// What each HapticType feels like. Strengths use three tiers, the same ones Nice Vibrations' presets used
/// (about 0.16, 0.47 and 1.0 of the motor): light, medium and full. Impacts are one pulse; notifications
/// are short patterns the player learns to read (two rising taps = good, two medium taps = careful,
/// three hard taps = bad).
/// Single pulses also name the closest Android system effect, which feels best where the device has it.
/// </summary>
public static class HapticPresets
{
    public const int Light = 40;   // ~0.156
    public const int Medium = 120; // ~0.471
    public const int Full = HapticPattern.MaxAmplitude;

    private static readonly HapticPattern SelectionPattern =
        HapticPattern.OneShot(12, Light, HapticPattern.EffectTick);

    private static readonly HapticPattern LightImpactPattern =
        HapticPattern.OneShot(20, Light, HapticPattern.EffectClick);

    private static readonly HapticPattern MediumImpactPattern =
        HapticPattern.OneShot(35, Medium);

    private static readonly HapticPattern HeavyImpactPattern =
        HapticPattern.OneShot(50, Full, HapticPattern.EffectHeavyClick);

    // tap ... TAP
    private static readonly HapticPattern SuccessPattern = new HapticPattern(
        new long[] { 30, 90, 50 },
        new[] { Medium, 0, Full });

    // tap . tap (medium: a heads-up, not an alarm)
    private static readonly HapticPattern WarningPattern = new HapticPattern(
        new long[] { 30, 70, 30 },
        new[] { Medium, 0, Medium });

    // TAP . TAP . TAP
    private static readonly HapticPattern FailurePattern = new HapticPattern(
        new long[] { 50, 70, 50, 70, 80 },
        new[] { Full, 0, Full, 0, Full });

    /// <summary>The pattern for a type, or null for None (nothing to play).</summary>
    public static HapticPattern Get(HapticType type)
    {
        switch (type)
        {
            case HapticType.Selection: return SelectionPattern;
            case HapticType.LightImpact: return LightImpactPattern;
            case HapticType.MediumImpact: return MediumImpactPattern;
            case HapticType.HeavyImpact: return HeavyImpactPattern;
            case HapticType.Success: return SuccessPattern;
            case HapticType.Warning: return WarningPattern;
            case HapticType.Failure: return FailurePattern;
            default: return null;
        }
    }
}
