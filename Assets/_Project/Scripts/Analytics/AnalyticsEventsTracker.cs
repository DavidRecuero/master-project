using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns the game events (GameEvents) into analytics events, so the gameplay knows nothing about analytics.
/// Everything goes through IAnalyticsService, which drops it all without consent.
///
/// The measuring plan:
///   Progression  Start / Complete / Fail "Level_012" (abandoning counts as Fail, plus the design event
///                Level:Abandoned with the level as value). Levels forced with Tools > Level Loader are not sent.
///   Resources    Coins only. Source: Reward/LevelWin. Sink: Booster/Match|Shuffle|TrayClearer, Life/Single|Refill.
///                The currency and item types are declared in Resources/GameAnalytics/Settings (Events tab):
///                GameAnalytics rejects resource events that are not declared there.
///   Design       Booster:Used:{Type}:Free|Coins, Booster:Denied:{Type}, Lives:OutOfLives.
/// Errors and crashes are sent by the SDK itself (Settings > Advanced > Health tracking).
///
/// One shared tracker for the whole session, started by the Boot (StartShared).
/// </summary>
public sealed class AnalyticsEventsTracker
{
    public const string Currency = "Coins";
    public const string ItemReward = "Reward";
    public const string ItemBooster = "Booster";
    public const string ItemLife = "Life";

    private static AnalyticsEventsTracker _shared;

    private readonly IAnalyticsService _analytics;
    private readonly Func<int?> _levelBeingPlayed;
    private readonly HashSet<BoosterType> _paidWithCoins = new HashSet<BoosterType>();
    private int? _level;
    private bool _started;

    /// <param name="levelBeingPlayed">The level of the scene, or null when it must not be measured (forced level, no game). Read when a level loads.</param>
    public AnalyticsEventsTracker(IAnalyticsService analytics, Func<int?> levelBeingPlayed = null)
    {
        _analytics = analytics;
        _levelBeingPlayed = levelBeingPlayed ?? LevelFromGameManager;
    }

    /// <summary>Starts the shared tracker (Boot). A previous one stops, so events are never counted twice.</summary>
    public static void StartShared(IAnalyticsService analytics)
    {
        _shared?.Stop();
        _shared = new AnalyticsEventsTracker(analytics);
        _shared.Start();
    }

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShared()
    {
        _shared?.Stop();
        _shared = null;
    }

    /// <summary>Level names sort well in the dashboard: Level_001, Level_012, Level_120.</summary>
    public static string LevelName(int level) => $"Level_{level:000}";

    public void Start()
    {
        if (_started) return;
        _started = true;

        GameEvents.OnLevelLoaded += OnLevelLoaded;
        GameEvents.OnLevelCleared += OnLevelCleared;
        GameEvents.OnLevelFailed += OnLevelFailed;
        GameEvents.OnLevelAbandoned += OnLevelAbandoned;
        GameEvents.OnLevelRewarded += OnLevelRewarded;
        GameEvents.OnBoosterPurchased += OnBoosterPurchased;
        GameEvents.OnBoosterUsed += OnBoosterUsed;
        GameEvents.OnBoosterDenied += OnBoosterDenied;
        GameEvents.OnLivesBought += OnLivesBought;
        GameEvents.OnOutOfLives += OnOutOfLives;
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;

        GameEvents.OnLevelLoaded -= OnLevelLoaded;
        GameEvents.OnLevelCleared -= OnLevelCleared;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLevelAbandoned -= OnLevelAbandoned;
        GameEvents.OnLevelRewarded -= OnLevelRewarded;
        GameEvents.OnBoosterPurchased -= OnBoosterPurchased;
        GameEvents.OnBoosterUsed -= OnBoosterUsed;
        GameEvents.OnBoosterDenied -= OnBoosterDenied;
        GameEvents.OnLivesBought -= OnLivesBought;
        GameEvents.OnOutOfLives -= OnOutOfLives;
    }

    // ---------- Progression ----------

    private void OnLevelLoaded(LevelData data)
    {
        _level = _levelBeingPlayed();
        _paidWithCoins.Clear();
        if (_level.HasValue) _analytics.Progression(AnalyticsProgressionStatus.Start, LevelName(_level.Value));
    }

    private void OnLevelCleared() => EndLevel(AnalyticsProgressionStatus.Complete);

    private void OnLevelFailed() => EndLevel(AnalyticsProgressionStatus.Fail);

    private void OnLevelAbandoned()
    {
        if (_level.HasValue) _analytics.Design("Level:Abandoned", _level.Value);
        EndLevel(AnalyticsProgressionStatus.Fail);
    }

    // Each level ends once: a later result event of the same attempt is ignored
    private void EndLevel(AnalyticsProgressionStatus status)
    {
        if (!_level.HasValue) return;
        _analytics.Progression(status, LevelName(_level.Value));
        _level = null;
    }

    // ---------- Resources ----------

    private void OnLevelRewarded(int coins) =>
        _analytics.Resource(AnalyticsResourceFlow.Source, Currency, coins, ItemReward, "LevelWin");

    private void OnBoosterPurchased(BoosterType type, int price)
    {
        _paidWithCoins.Add(type);
        _analytics.Resource(AnalyticsResourceFlow.Sink, Currency, price, ItemBooster, type.ToString());
    }

    private void OnLivesBought(int lives, int price) =>
        _analytics.Resource(AnalyticsResourceFlow.Sink, Currency, price, ItemLife, lives == 1 ? "Single" : "Refill");

    // ---------- Design ----------

    private void OnBoosterUsed(BoosterType type)
    {
        // OnBoosterPurchased comes right before OnBoosterUsed when the booster was paid with coins
        string payment = _paidWithCoins.Remove(type) ? "Coins" : "Free";
        _analytics.Design($"Booster:Used:{type}:{payment}");
    }

    private void OnBoosterDenied(BoosterType type) => _analytics.Design($"Booster:Denied:{type}");

    private void OnOutOfLives() => _analytics.Design("Lives:OutOfLives");

    // ---------- Defaults ----------

    private static int? LevelFromGameManager()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.IsDebugLevel) return null;
        return game.CurrentLevelIndex;
    }
}
