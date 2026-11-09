using System;

/// <summary>
/// What winning a level gives. Pure logic, so the rule can grow (per level, per difficulty, Remote Config)
/// without touching who pays it out. Today: the same coins for every win, loop levels included.
/// </summary>
public sealed class LevelRewards
{
    private readonly int _coinsPerWin;

    public LevelRewards(int coinsPerWin)
    {
        _coinsPerWin = Math.Max(0, coinsPerWin);
    }

    public static LevelRewards From(RewardsConfig config) =>
        new LevelRewards(config != null ? config.coinsPerWin : RewardsConfig.DefaultCoinsPerWin);

    /// <summary>Coins for winning this level.</summary>
    public int CoinsForWin(int level) => _coinsPerWin;
}
