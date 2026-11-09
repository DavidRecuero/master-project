using System;

/// <summary>
/// Decides whether a win is a good moment to ask for the review card. Pure logic.
///   - Never before RateUsRules.FirstPromptLevel (the player needs to know the game first).
///   - At most RateUsRules.MaxPrompts times.
///   - After the first time, only if RateUsRules.DaysBetweenPrompts have passed since the last one.
/// No question is asked before the card: Google Play doesn't allow it.
/// </summary>
public static class RateUsPolicy
{
    public static bool ShouldAsk(int wonLevel, int promptsSoFar, DateTime? lastPromptUtc, DateTime utcNow, RateUsRules rules)
    {
        if (promptsSoFar >= rules.MaxPrompts) return false;
        if (wonLevel < rules.FirstPromptLevel) return false;
        if (promptsSoFar == 0 || lastPromptUtc == null) return true;

        // Clock moved back: the last request looks "in the future"; wait, as if it was just now
        return utcNow - lastPromptUtc.Value >= TimeSpan.FromDays(rules.DaysBetweenPrompts);
    }
}
