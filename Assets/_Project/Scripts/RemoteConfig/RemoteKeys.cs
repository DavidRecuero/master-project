/// <summary>
/// Keys of the Remote Config settings. The same names are in the Unity Dashboard (Remote Config) and in
/// Config/RemoteConfig/GameTuning.rc, the file that deploys them (Services > Deployment).
/// A key that is missing on the server keeps the value of the local config asset (LivesConfig, BoosterDefinition...).
/// </summary>
public static class RemoteKeys
{
    // Lives (LivesConfig)
    public const string LivesMax = "lives_max";
    public const string LivesMinutesPerLife = "lives_minutes_per_life";
    public const string LivesCoinsPerLife = "lives_coins_per_life";

    // Level rewards (RewardsConfig)
    public const string RewardCoinsPerWin = "reward_coins_per_win";

    // Rate Us (RateUsConfig)
    public const string RateUsFirstLevel = "rateus_first_level";
    public const string RateUsMaxPrompts = "rateus_max_prompts";
    public const string RateUsDaysBetween = "rateus_days_between";
    public const string RateUsDelaySeconds = "rateus_delay_seconds";

    // Kill switches
    public const string AnalyticsEnabled = "analytics_enabled";

    /// <summary>Coins one use costs once the free stock runs out, e.g. "booster_match_price".</summary>
    public static string BoosterPrice(BoosterType type) => $"booster_{Slug(type)}_price";

    /// <summary>Free uses a new player starts with, e.g. "booster_trayclearer_starter". Only new players get it.</summary>
    public static string BoosterStarter(BoosterType type) => $"booster_{Slug(type)}_starter";

    private static string Slug(BoosterType type) => type.ToString().ToLowerInvariant();
}
