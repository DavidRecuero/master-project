using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Remote settings over the local config assets: only existing, valid keys change a number.</summary>
public class GameTuningTests
{
    private static readonly LivesRules LocalLives = new LivesRules(5, 30 * 60, 20);
    private static readonly RateUsRules LocalRateUs = new RateUsRules(8, 3, 30, 0f);

    [Test]
    public void NoRemoteValues_KeepsEveryLocalValue()
    {
        LivesRules lives = GameTuning.Lives(RemoteValues.Empty, LocalLives);
        RateUsRules rateUs = GameTuning.RateUs(null, LocalRateUs);

        Assert.AreEqual(LocalLives, lives);
        Assert.AreEqual(LocalRateUs, rateUs);
        Assert.AreEqual(4, GameTuning.CoinsPerWin(RemoteValues.Empty, 4));
        Assert.AreEqual(15, GameTuning.BoosterPrice(RemoteValues.Empty, BoosterType.Match, 15));
        Assert.AreEqual(20, GameTuning.BoosterStarter(RemoteValues.Empty, BoosterType.Match, 20));
        Assert.IsTrue(GameTuning.AnalyticsEnabled(RemoteValues.Empty, true));
    }

    [Test]
    public void Lives_TakesEachRemoteKey_MinutesAsSeconds()
    {
        RemoteValues remote = RemoteTestValues.Of(RemoteKeys.LivesMax, "3", RemoteKeys.LivesMinutesPerLife, "10", RemoteKeys.LivesCoinsPerLife, "50");

        LivesRules lives = GameTuning.Lives(remote, LocalLives);

        Assert.AreEqual(3, lives.MaxLives);
        Assert.AreEqual(600, lives.SecondsPerLife);
        Assert.AreEqual(50, lives.CoinsPerLife);
    }

    [Test]
    public void Lives_OnlyTheKeysThatExistChange()
    {
        LivesRules lives = GameTuning.Lives(RemoteTestValues.Of(RemoteKeys.LivesCoinsPerLife, "0"), LocalLives);

        Assert.AreEqual(5, lives.MaxLives);
        Assert.AreEqual(30 * 60, lives.SecondsPerLife);
        Assert.AreEqual(0, lives.CoinsPerLife, "Free lives are a valid remote choice (an event)");
    }

    [Test]
    public void OutOfRangeValue_IsIgnored_NotClamped()
    {
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("lives_max"));
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("lives_minutes_per_life"));

        RemoteValues remote = RemoteTestValues.Of(RemoteKeys.LivesMax, "0", RemoteKeys.LivesMinutesPerLife, "abc");
        LivesRules lives = GameTuning.Lives(remote, LocalLives);

        Assert.AreEqual(5, lives.MaxLives);
        Assert.AreEqual(30 * 60, lives.SecondsPerLife);
    }

    [Test]
    public void RateUs_TakesTheRemoteKeys()
    {
        RemoteValues remote = RemoteTestValues.Of(RemoteKeys.RateUsFirstLevel, "12", RemoteKeys.RateUsMaxPrompts, "1",
            RemoteKeys.RateUsDaysBetween, "60", RemoteKeys.RateUsDelaySeconds, "1.5");

        RateUsRules rules = GameTuning.RateUs(remote, LocalRateUs);

        Assert.AreEqual(12, rules.FirstPromptLevel);
        Assert.AreEqual(1, rules.MaxPrompts);
        Assert.AreEqual(60, rules.DaysBetweenPrompts);
        Assert.AreEqual(1.5f, rules.DelayAfterWinSeconds);
    }

    [Test]
    public void Boosters_EachTypeHasItsOwnKeys()
    {
        RemoteValues remote = RemoteTestValues.Of("booster_trayclearer_price", "8", "booster_shuffle_starter", "3");

        Assert.AreEqual(8, GameTuning.BoosterPrice(remote, BoosterType.TrayClearer, 5));
        Assert.AreEqual(10, GameTuning.BoosterPrice(remote, BoosterType.Shuffle, 10));
        Assert.AreEqual(3, GameTuning.BoosterStarter(remote, BoosterType.Shuffle, 25));
    }

    [Test]
    public void Rewards_TakesTheRemoteKey()
    {
        Assert.AreEqual(10, GameTuning.CoinsPerWin(RemoteTestValues.Of(RemoteKeys.RewardCoinsPerWin, "10"), 4));
    }

    [Test]
    public void AnalyticsKillSwitch_CanOnlyBeReadFromTheKey()
    {
        Assert.IsFalse(GameTuning.AnalyticsEnabled(RemoteTestValues.Of(RemoteKeys.AnalyticsEnabled, "false"), true));
        Assert.IsTrue(GameTuning.AnalyticsEnabled(RemoteTestValues.Of(RemoteKeys.AnalyticsEnabled, "nonsense"), true));
    }

    [Test]
    public void KeyNames_MatchTheDashboard()
    {
        Assert.AreEqual("booster_match_price", RemoteKeys.BoosterPrice(BoosterType.Match));
        Assert.AreEqual("booster_trayclearer_starter", RemoteKeys.BoosterStarter(BoosterType.TrayClearer));
    }
}
