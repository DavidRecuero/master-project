using System;

/// <summary>
/// When the Google Play review card may show. A plain value so the logic is tested without assets;
/// the designers tune it in RateUsConfig (later, Remote Config can fill the same value).
/// </summary>
[Serializable]
public struct RateUsRules
{
    public int FirstPromptLevel;
    public int MaxPrompts;
    public int DaysBetweenPrompts;
    public float DelayAfterWinSeconds;

    public RateUsRules(int firstPromptLevel, int maxPrompts, int daysBetweenPrompts, float delayAfterWinSeconds)
    {
        FirstPromptLevel = Math.Max(1, firstPromptLevel);
        MaxPrompts = Math.Max(0, maxPrompts);
        DaysBetweenPrompts = Math.Max(0, daysBetweenPrompts);
        DelayAfterWinSeconds = Math.Max(0f, delayAfterWinSeconds);
    }

    /// <summary>From level 8, at most 3 times, 30 days apart, as soon as the win popup shows up.</summary>
    public static RateUsRules Default => new RateUsRules(8, 3, 30, 0f);
}
