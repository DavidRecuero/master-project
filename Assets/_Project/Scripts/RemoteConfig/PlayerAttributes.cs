using System;

/// <summary>
/// What the game tells Remote Config about the player, so the Game Overrides can target segments with JEXL
/// conditions on the dashboard (e.g. "user.failStreak >= 3", "user.inLoop == true"). The field names are the names
/// in the conditions: renaming one breaks the overrides that use it.
///
/// Only gameplay numbers, no personal data: they stay in Unity Gaming Services (no consent needed, like the
/// sign-in and the cloud save) and are never sent to analytics.
///
/// Days are UTC days. The activity fields describe the player BEFORE this launch (the Boot reads them first and
/// registers the session after), so a player coming back after a week is "7 days since last play" on that launch.
/// </summary>
[Serializable]
public struct PlayerAttributes
{
    /// <summary>Level the player is on (CurrentLevel).</summary>
    public int level;

    /// <summary>True when the level is past the last designed one (boards come from the level loop).</summary>
    public bool inLoop;

    /// <summary>Whole days since the player started (0 on the first day).</summary>
    public int daysSinceInstall;

    /// <summary>Whole days since the previous launch (0 on the first launch or the same day).</summary>
    public int daysSinceLastPlay;

    /// <summary>Different days the player has launched the game before this launch.</summary>
    public int daysPlayed;

    /// <summary>Levels lost or abandoned in a row since the last win.</summary>
    public int failStreak;

    /// <summary>Coin balance.</summary>
    public int coins;

    /// <summary>No profile (tests, game not started from the Boot): a new player.</summary>
    public static PlayerAttributes NewPlayer => new PlayerAttributes { level = 1 };

    /// <param name="designedLevels">Number of designed levels (where the loop starts). 0 or less: unknown, never in the loop.</param>
    public static PlayerAttributes From(UserProfile profile, DateTime utcNow, int designedLevels)
    {
        if (profile == null) return NewPlayer;

        int level = Math.Max(1, profile.CurrentLevel);
        return new PlayerAttributes
        {
            level = level,
            inLoop = designedLevels > 0 && level > designedLevels,
            daysSinceInstall = DaysSince(profile.CreatedAtTicks, utcNow),
            daysSinceLastPlay = DaysSince(profile.LastSessionTicks, utcNow),
            daysPlayed = Math.Max(0, profile.DaysPlayed),
            failStreak = Math.Max(0, profile.FailStreak),
            coins = Math.Max(0, profile.Coins),
        };
    }

    // A missing or future date (clock moved back) counts as today
    private static int DaysSince(long ticks, DateTime utcNow)
    {
        if (ticks <= 0 || ticks > utcNow.Ticks) return 0;
        return (int)Math.Min(int.MaxValue, (utcNow.Ticks - ticks) / TimeSpan.TicksPerDay);
    }

    public override string ToString() =>
        $"level {level}{(inLoop ? " (loop)" : "")}, day {daysSinceInstall}, {daysSinceLastPlay} days since last play, " +
        $"{daysPlayed} days played, fail streak {failStreak}, coins {coins}";
}
