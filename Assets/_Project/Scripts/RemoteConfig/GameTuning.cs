using UnityEngine;

/// <summary>
/// Puts the remote settings over the local config assets. Pure: each system keeps its own asset as the
/// default, and only the keys that exist on the server change it.
///
/// Safety net: a remote value out of range (a typo in the dashboard, "lives_max": 0) is ignored with a warning
/// and the local value stays. It is not clamped: half-applying a wrong number is worse than not applying it.
/// </summary>
public static class GameTuning
{
    public static LivesRules Lives(RemoteValues remote, LivesRules local)
    {
        int maxLives = Ranged(remote, RemoteKeys.LivesMax, local.MaxLives, 1, 99);
        int coinsPerLife = Ranged(remote, RemoteKeys.LivesCoinsPerLife, local.CoinsPerLife, 0, 100000);

        // Minutes on the server, seconds in the game. Without a valid key, the local seconds exactly
        int secondsPerLife = TryRanged(remote, RemoteKeys.LivesMinutesPerLife, 1, 24 * 60, out int minutes)
            ? minutes * 60
            : local.SecondsPerLife;

        return new LivesRules(maxLives, secondsPerLife, coinsPerLife);
    }

    public static int CoinsPerWin(RemoteValues remote, int local) =>
        Ranged(remote, RemoteKeys.RewardCoinsPerWin, local, 0, 10000);

    public static RateUsRules RateUs(RemoteValues remote, RateUsRules local) =>
        new RateUsRules(
            Ranged(remote, RemoteKeys.RateUsFirstLevel, local.FirstPromptLevel, 1, 10000),
            Ranged(remote, RemoteKeys.RateUsMaxPrompts, local.MaxPrompts, 0, 100),
            Ranged(remote, RemoteKeys.RateUsDaysBetween, local.DaysBetweenPrompts, 0, 3650),
            RangedFloat(remote, RemoteKeys.RateUsDelaySeconds, local.DelayAfterWinSeconds, 0f, 60f));

    public static int BoosterPrice(RemoteValues remote, BoosterType type, int local) =>
        Ranged(remote, RemoteKeys.BoosterPrice(type), local, 0, 100000);

    public static int BoosterStarter(RemoteValues remote, BoosterType type, int local) =>
        Ranged(remote, RemoteKeys.BoosterStarter(type), local, 0, 1000);

    /// <summary>Remote kill switch for analytics. On top of the inspector one and of the player's consent, never instead.</summary>
    public static bool AnalyticsEnabled(RemoteValues remote, bool local) =>
        (remote ?? RemoteValues.Empty).GetBool(RemoteKeys.AnalyticsEnabled, local);

    // ---------- Helpers ----------

    private static int Ranged(RemoteValues remote, string key, int local, int min, int max) =>
        TryRanged(remote, key, min, max, out int value) ? value : local;

    /// <summary>True with the remote value if the key exists and is a whole number in range. Warns if it exists but isn't.</summary>
    private static bool TryRanged(RemoteValues remote, string key, int min, int max, out int value)
    {
        value = 0;
        if (remote == null || !remote.Has(key)) return false;

        int read = remote.GetInt(key, int.MinValue);
        if (read >= min && read <= max)
        {
            value = read;
            return true;
        }

        Warn(remote, key, min, max);
        return false;
    }

    private static float RangedFloat(RemoteValues remote, string key, float local, float min, float max)
    {
        if (remote == null || !remote.Has(key)) return local;

        float value = remote.GetFloat(key, float.NaN);
        if (!float.IsNaN(value) && value >= min && value <= max) return value;

        Warn(remote, key, min, max);
        return local;
    }

    private static void Warn(RemoteValues remote, string key, object min, object max) =>
        Debug.LogWarning($"[RemoteConfig] '{key}' = '{remote.GetString(key, "")}' is not valid ({min}-{max}). Keeping the local value.");
}
