using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Records the analytics calls as text. Always "active": consent is tested in AnalyticsServiceTests.</summary>
public class RecordingAnalyticsService : IAnalyticsService
{
    public readonly List<string> Calls = new List<string>();
    public bool IsActive => true;
    public void Start() { }
    public void Stop() { }
    public void SetAppPaused(bool paused) { }

    public void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null) =>
        Calls.Add($"progression:{status}:{progression01}");

    public void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId) =>
        Calls.Add($"resource:{flow}:{currency}:{amount}:{itemType}:{itemId}");

    public void Design(string eventId, float? value = null) =>
        Calls.Add(value.HasValue ? $"design:{eventId}:{value.Value}" : $"design:{eventId}");
}

/// <summary>The measuring plan: which game event becomes which analytics event.</summary>
public class AnalyticsEventsTrackerTests
{
    private RecordingAnalyticsService _analytics;
    private AnalyticsEventsTracker _tracker;
    private int? _level;

    [SetUp]
    public void SetUp()
    {
        _analytics = new RecordingAnalyticsService();
        _level = 12;
        _tracker = new AnalyticsEventsTracker(_analytics, () => _level);
        _tracker.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _tracker.Stop();
    }

    private void StartLevel() => GameEvents.TriggerLevelLoaded(null);

    // ---------- Progression ----------

    [Test]
    public void LevelName_IsPaddedSoItSortsWell()
    {
        Assert.AreEqual("Level_001", AnalyticsEventsTracker.LevelName(1));
        Assert.AreEqual("Level_012", AnalyticsEventsTracker.LevelName(12));
        Assert.AreEqual("Level_120", AnalyticsEventsTracker.LevelName(120));
    }

    [Test]
    public void Win_StartAndComplete()
    {
        StartLevel();
        GameEvents.TriggerLevelCleared();

        CollectionAssert.AreEqual(new[] { "progression:Start:Level_012", "progression:Complete:Level_012" }, _analytics.Calls);
    }

    [Test]
    public void Lose_StartAndFail()
    {
        StartLevel();
        GameEvents.TriggerLevelFailed();

        CollectionAssert.AreEqual(new[] { "progression:Start:Level_012", "progression:Fail:Level_012" }, _analytics.Calls);
    }

    [Test]
    public void Abandon_FailPlusDesignEventWithTheLevel()
    {
        StartLevel();
        GameEvents.TriggerLevelAbandoned();

        CollectionAssert.AreEqual(new[]
        {
            "progression:Start:Level_012",
            "design:Level:Abandoned:12",
            "progression:Fail:Level_012",
        }, _analytics.Calls);
    }

    [Test]
    public void ForcedLevel_IsNotMeasured()
    {
        _level = null;   // Tools > Level Loader

        StartLevel();
        GameEvents.TriggerLevelCleared();

        Assert.IsEmpty(_analytics.Calls);
    }

    [Test]
    public void ResultWithoutAStartedLevel_IsIgnored()
    {
        GameEvents.TriggerLevelFailed();

        Assert.IsEmpty(_analytics.Calls);
    }

    [Test]
    public void EachAttempt_EndsOnce()
    {
        StartLevel();
        GameEvents.TriggerLevelFailed();
        GameEvents.TriggerLevelFailed();

        Assert.AreEqual(2, _analytics.Calls.Count);
    }

    [Test]
    public void Retry_IsANewAttempt()
    {
        StartLevel();
        GameEvents.TriggerLevelFailed();
        StartLevel();
        GameEvents.TriggerLevelCleared();

        CollectionAssert.AreEqual(new[]
        {
            "progression:Start:Level_012", "progression:Fail:Level_012",
            "progression:Start:Level_012", "progression:Complete:Level_012",
        }, _analytics.Calls);
    }

    // ---------- Resources ----------

    [Test]
    public void LevelReward_IsACoinSource()
    {
        GameEvents.TriggerLevelRewarded(20);

        CollectionAssert.AreEqual(new[] { "resource:Source:Coins:20:Reward:LevelWin" }, _analytics.Calls);
    }

    [Test]
    public void BoosterPaidWithCoins_SinkAndUsedWithCoins()
    {
        GameEvents.TriggerBoosterPurchased(BoosterType.Shuffle, 40);
        GameEvents.TriggerBoosterUsed(BoosterType.Shuffle);

        CollectionAssert.AreEqual(new[]
        {
            "resource:Sink:Coins:40:Booster:Shuffle",
            "design:Booster:Used:Shuffle:Coins",
        }, _analytics.Calls);
    }

    [Test]
    public void BoosterFromFreeStock_UsedFree()
    {
        GameEvents.TriggerBoosterUsed(BoosterType.Match);

        CollectionAssert.AreEqual(new[] { "design:Booster:Used:Match:Free" }, _analytics.Calls);
    }

    [Test]
    public void PaymentIsOnlyForThatUse()
    {
        GameEvents.TriggerBoosterPurchased(BoosterType.Shuffle, 40);
        GameEvents.TriggerBoosterUsed(BoosterType.Shuffle);
        GameEvents.TriggerBoosterUsed(BoosterType.Shuffle);

        Assert.AreEqual("design:Booster:Used:Shuffle:Free", _analytics.Calls[2]);
    }

    [TestCase(1, 20, "Single")]
    [TestCase(4, 80, "Refill")]
    public void LivesBought_AreACoinSink(int lives, int price, string itemId)
    {
        GameEvents.TriggerLivesBought(lives, price);

        CollectionAssert.AreEqual(new[] { $"resource:Sink:Coins:{price}:Life:{itemId}" }, _analytics.Calls);
    }

    // ---------- Design ----------

    [Test]
    public void BoosterDenied_IsADesignEvent()
    {
        GameEvents.TriggerBoosterDenied(BoosterType.TrayClearer);

        CollectionAssert.AreEqual(new[] { "design:Booster:Denied:TrayClearer" }, _analytics.Calls);
    }

    [Test]
    public void OutOfLives_IsADesignEvent()
    {
        GameEvents.TriggerOutOfLives();

        CollectionAssert.AreEqual(new[] { "design:Lives:OutOfLives" }, _analytics.Calls);
    }

    // ---------- Start / Stop ----------

    [Test]
    public void AfterStop_NothingIsSent()
    {
        _tracker.Stop();

        StartLevel();
        GameEvents.TriggerLevelRewarded(20);

        Assert.IsEmpty(_analytics.Calls);
    }

    [Test]
    public void StartTwice_DoesNotCountTwice()
    {
        _tracker.Start();

        GameEvents.TriggerOutOfLives();

        Assert.AreEqual(1, _analytics.Calls.Count);
    }
}

/// <summary>The new game events the analytics need are raised where the thing happens.</summary>
public class AnalyticsSourceEventsTests
{
    private readonly List<string> _events = new List<string>();

    private void OnPurchased(BoosterType type, int price) => _events.Add($"booster:{type}:{price}");
    private void OnLivesBought(int lives, int price) => _events.Add($"lives:{lives}:{price}");
    private void OnOutOfLives() => _events.Add("outoflives");

    [SetUp]
    public void SetUp()
    {
        _events.Clear();
        GameEvents.OnBoosterPurchased += OnPurchased;
        GameEvents.OnLivesBought += OnLivesBought;
        GameEvents.OnOutOfLives += OnOutOfLives;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.OnBoosterPurchased -= OnPurchased;
        GameEvents.OnLivesBought -= OnLivesBought;
        GameEvents.OnOutOfLives -= OnOutOfLives;
    }

    private LivesService LivesWithMissing(int missing, out UserProfile profile)
    {
        var p = new UserProfile();
        var lives = new LivesService(new LivesRules(5, 30 * 60, 20), () => p, new FakeUserDataProvider { Coins = 500 }, null, new FakeClock());
        for (int i = 0; i < missing; i++) lives.TryUseLife();
        profile = p;
        return lives;
    }

    [Test]
    public void BuyingOneLife_RaisesLivesBought()
    {
        LivesWithMissing(3, out _).TryBuyLife();

        CollectionAssert.AreEqual(new[] { "lives:1:20" }, _events);
    }

    [Test]
    public void Refill_RaisesLivesBoughtWithAllTheMissing()
    {
        LivesWithMissing(3, out _).TryRefill();

        CollectionAssert.AreEqual(new[] { "lives:3:60" }, _events);
    }

    [Test]
    public void FailedPurchase_RaisesNothing()
    {
        var p = new UserProfile();
        var lives = new LivesService(new LivesRules(5, 30 * 60, 20), () => p, new FakeUserDataProvider { Coins = 5 }, null, new FakeClock());
        lives.TryUseLife();

        Assert.IsFalse(lives.TryBuyLife());
        Assert.IsEmpty(_events);
    }

    [Test]
    public void NoLives_RaisesOutOfLives()
    {
        var prefab = PopupTestUtils.MakePrefab<NoLivesPopup>("NoLivesPopup");
        try
        {
            new LivesGate(new FakeLivesService { Lives = 0 }, new FakePopupService(), prefab).TryEnter(() => { });
            CollectionAssert.AreEqual(new[] { "outoflives" }, _events);
        }
        finally
        {
            Object.DestroyImmediate(prefab.gameObject);
        }
    }

    [Test]
    public void WithLives_DoesNotRaiseOutOfLives()
    {
        new LivesGate(new FakeLivesService { Lives = 3 }, new FakePopupService(), null).TryEnter(() => { });

        Assert.IsEmpty(_events);
    }
}
