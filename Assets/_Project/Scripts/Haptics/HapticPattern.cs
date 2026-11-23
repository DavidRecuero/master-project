using System.Collections.Generic;

/// <summary>
/// What one vibration feels like, in a device-neutral form: a list of segments (milliseconds and strength 0..255,
/// where 0 is a pause) plus, optionally, the Android system effect that best matches it.
/// The backend picks what the device can do: the waveform with strengths (Android 8+ with amplitude control),
/// the system effect (Android 10+ without amplitude control), or just on/off timings (any vibrator).
/// Pure C#, so the presets and the conversions are testable in EditMode.
/// </summary>
public sealed class HapticPattern
{
    /// <summary>No Android system effect for this pattern.</summary>
    public const int NoPredefinedEffect = -1;

    // android.os.VibrationEffect constants (API 29+)
    public const int EffectClick = 0;
    public const int EffectDoubleClick = 1;
    public const int EffectTick = 2;
    public const int EffectHeavyClick = 5;

    public const int MaxAmplitude = 255;

    public readonly long[] Timings;
    public readonly int[] Amplitudes;
    public readonly int PredefinedEffect;

    public HapticPattern(long[] timings, int[] amplitudes, int predefinedEffect = NoPredefinedEffect)
    {
        Timings = timings ?? new long[0];
        Amplitudes = amplitudes ?? new int[0];
        PredefinedEffect = predefinedEffect;
    }

    /// <summary>A single pulse.</summary>
    public static HapticPattern OneShot(long milliseconds, int amplitude, int predefinedEffect = NoPredefinedEffect)
        => new HapticPattern(new[] { milliseconds }, new[] { amplitude }, predefinedEffect);

    public bool IsSinglePulse => Timings.Length == 1;

    public bool HasPredefinedEffect => PredefinedEffect != NoPredefinedEffect;

    /// <summary>Total length in milliseconds, pauses included.</summary>
    public long TotalMilliseconds
    {
        get
        {
            long total = 0;
            foreach (long t in Timings) total += t;
            return total;
        }
    }

    /// <summary>Same number of timings and strengths, nothing negative, strengths within 0..255, at least one pulse.</summary>
    public bool IsValid
    {
        get
        {
            if (Timings.Length == 0 || Timings.Length != Amplitudes.Length) return false;

            bool anyPulse = false;
            for (int i = 0; i < Timings.Length; i++)
            {
                if (Timings[i] < 0 || Amplitudes[i] < 0 || Amplitudes[i] > MaxAmplitude) return false;
                if (Timings[i] > 0 && Amplitudes[i] > 0) anyPulse = true;
            }
            return anyPulse;
        }
    }

    /// <summary>
    /// The pattern as Android's on/off timings (for vibrators without amplitude control and for Android before 8):
    /// the list always starts with an "off" duration and then alternates on, off, on...
    /// Consecutive segments in the same state are merged, and strengths are dropped (on is on).
    /// </summary>
    public long[] ToOnOffTimings()
    {
        var result = new List<long> { 0 };
        bool currentOn = false;

        for (int i = 0; i < Timings.Length; i++)
        {
            bool on = Amplitudes[i] > 0;
            if (on == currentOn)
            {
                result[result.Count - 1] += Timings[i];
            }
            else
            {
                result.Add(Timings[i]);
                currentOn = on;
            }
        }

        return result.ToArray();
    }
}
