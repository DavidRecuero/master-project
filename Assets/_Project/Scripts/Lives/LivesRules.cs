using System;

/// <summary>
/// Numbers of the lives system: how many lives, how long one takes to come back and what one costs.
/// A plain value so the logic can be tested without assets. The designers tune it in LivesConfig.
/// </summary>
[Serializable]
public struct LivesRules
{
    public int MaxLives;
    public int SecondsPerLife;
    public int CoinsPerLife;

    public LivesRules(int maxLives, int secondsPerLife, int coinsPerLife)
    {
        MaxLives = Math.Max(1, maxLives);
        SecondsPerLife = Math.Max(1, secondsPerLife);
        CoinsPerLife = Math.Max(0, coinsPerLife);
    }

    /// <summary>5 lives, one every 30 minutes, 20 coins each.</summary>
    public static LivesRules Default => new LivesRules(5, 30 * 60, 20);

    public TimeSpan TimePerLife => TimeSpan.FromSeconds(SecondsPerLife);
}
