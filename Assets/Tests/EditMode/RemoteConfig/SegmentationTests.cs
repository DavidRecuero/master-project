using System;
using System.Threading;
using NUnit.Framework;

/// <summary>
/// Segmentation: the attributes the game sends to Remote Config, the groups it reads back (segment, A/B variant,
/// lifecycle), and how they reach the analytics (only with consent, only known values).
/// </summary>
public class SegmentationTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
    private const int DesignedLevels = 38;

    // ---------- PlayerAttributes ----------

    [Test]
    public void Attributes_ComeFromTheProfile()
    {
        var profile = new UserProfile
        {
            CurrentLevel = 14,
            Coins = 120,
            FailStreak = 3,
            CreatedAtTicks = Now.AddDays(-5).AddHours(-3).Ticks,
            LastSessionTicks = Now.AddDays(-8).Ticks,
            DaysPlayed = 4,
        };

        PlayerAttributes attributes = PlayerAttributes.From(profile, Now, DesignedLevels);

        Assert.AreEqual(14, attributes.level);
        Assert.IsFalse(attributes.inLoop);
        Assert.AreEqual(120, attributes.coins);
        Assert.AreEqual(3, attributes.failStreak);
        Assert.AreEqual(5, attributes.daysSinceInstall, "Whole days");
        Assert.AreEqual(8, attributes.daysSinceLastPlay);
        Assert.AreEqual(4, attributes.daysPlayed);
    }

    [TestCase(38, false)]
    [TestCase(39, true)]
    public void InLoop_StartsAfterTheLastDesignedLevel(int level, bool inLoop)
    {
        var profile = new UserProfile { CurrentLevel = level };

        Assert.AreEqual(inLoop, PlayerAttributes.From(profile, Now, DesignedLevels).inLoop);
    }

    [Test]
    public void InLoop_UnknownLevelCount_IsNeverInTheLoop()
    {
        var profile = new UserProfile { CurrentLevel = 500 };

        Assert.IsFalse(PlayerAttributes.From(profile, Now, 0).inLoop);
    }

    [Test]
    public void Attributes_NoProfile_IsANewPlayer()
    {
        PlayerAttributes attributes = PlayerAttributes.From(null, Now, DesignedLevels);

        Assert.AreEqual(1, attributes.level);
        Assert.AreEqual(0, attributes.daysSinceInstall);
        Assert.AreEqual(0, attributes.daysSinceLastPlay);
        Assert.AreEqual(0, attributes.failStreak);
    }

    [Test]
    public void Attributes_FutureOrMissingDates_AreToday()
    {
        var future = new UserProfile { CreatedAtTicks = Now.AddDays(3).Ticks, LastSessionTicks = Now.AddDays(1).Ticks };
        var missing = new UserProfile { CreatedAtTicks = 0, LastSessionTicks = 0 };

        Assert.AreEqual(0, PlayerAttributes.From(future, Now, DesignedLevels).daysSinceInstall);
        Assert.AreEqual(0, PlayerAttributes.From(future, Now, DesignedLevels).daysSinceLastPlay);
        Assert.AreEqual(0, PlayerAttributes.From(missing, Now, DesignedLevels).daysSinceInstall);
        Assert.AreEqual(0, PlayerAttributes.From(missing, Now, DesignedLevels).daysSinceLastPlay);
    }

    [Test]
    public void NewProfile_StartsTodayWithNoHistory()
    {
        var profile = new UserProfile();

        Assert.Greater(profile.CreatedAtTicks, 0);
        Assert.AreEqual(0, profile.FailStreak);
        Assert.AreEqual(0, profile.LastSessionTicks);
        Assert.AreEqual(0, profile.DaysPlayed);
    }

    [Test]
    public void OldSave_WithoutTheNewFields_GetsADate()
    {
        // A profile saved by 0.6.0 (no segmentation fields)
        UserProfile old = UnityEngine.JsonUtility.FromJson<UserProfile>("{\"UserId\":\"x\",\"CurrentLevel\":7,\"Coins\":40}");

        Assert.AreEqual(7, old.CurrentLevel);
        Assert.Greater(old.CreatedAtTicks, 0);
        Assert.AreEqual(0, old.FailStreak);
        Assert.AreEqual(0, old.DaysPlayed);
    }

    // ---------- The service sends them ----------

    [Test]
    public void Fetch_SendsTheCurrentAttributes()
    {
        SynchronizationContext previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var backend = new FakeRemoteConfigBackend();
            int level = 3;
            var service = new RemoteConfigService(backend, null, () => Now, () => new PlayerAttributes { level = level });

            level = 9;   // read at download time, not when the service was created
            service.FetchAsync().Wait();

            Assert.AreEqual(9, backend.LastAttributes.level);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    // ---------- PlayerSegments ----------

    [Test]
    public void NoKeys_DefaultGroups()
    {
        Assert.AreEqual(PlayerSegments.Default, PlayerSegments.Segment(RemoteValues.Empty));
        Assert.AreEqual(PlayerSegments.NoExperiment, PlayerSegments.Experiment(null));
        Assert.AreEqual(PlayerSegments.NoLifecycle, PlayerSegments.Lifecycle(RemoteValues.Empty));
    }

    [Test]
    public void KnownValues_AreRead_IgnoringCaseAndSpaces()
    {
        RemoteValues remote = RemoteTestValues.Of(RemoteKeys.Segment, " Returning ", RemoteKeys.Experiment, "B",
            RemoteKeys.Lifecycle, "EARLY");

        Assert.AreEqual(PlayerSegments.Returning, PlayerSegments.Segment(remote));
        Assert.AreEqual(PlayerSegments.VariantB, PlayerSegments.Experiment(remote));
        Assert.AreEqual(PlayerSegments.Early, PlayerSegments.Lifecycle(remote));
    }

    [Test]
    public void UnknownValues_NeverReachAnalytics()
    {
        RemoteValues remote = RemoteTestValues.Of(RemoteKeys.Segment, "whales", RemoteKeys.Experiment, "z",
            RemoteKeys.Lifecycle, "ancient");

        Assert.AreEqual(PlayerSegments.Other, PlayerSegments.Segment(remote));
        Assert.AreEqual(PlayerSegments.NoExperiment, PlayerSegments.Experiment(remote));
        Assert.AreEqual(PlayerSegments.NoLifecycle, PlayerSegments.Lifecycle(remote));
    }

    [Test]
    public void EveryValue_IsDeclaredInTheGameAnalyticsSettings()
    {
        // GameAnalytics drops dimension values that are not in its settings
        string settings = System.IO.File.ReadAllText("Assets/Resources/GameAnalytics/Settings.asset").Replace("\r", "");

        AssertDeclared(settings, "CustomDimensions01:", "CustomDimensions02:", PlayerSegments.Segments);
        AssertDeclared(settings, "CustomDimensions02:", "CustomDimensions03:", PlayerSegments.Variants);
        AssertDeclared(settings, "CustomDimensions03:", "ResourceItemTypes:", PlayerSegments.Lifecycles);
    }

    private static void AssertDeclared(string settings, string from, string to, string[] values)
    {
        int start = settings.IndexOf(from, StringComparison.Ordinal);
        int end = start < 0 ? -1 : settings.IndexOf(to, start + 1, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0 && end > start, $"'{from}' not found in the GameAnalytics settings");

        string section = settings.Substring(start, end - start);
        foreach (string value in values)
            StringAssert.Contains($"  - {value}\n", section, $"'{value}' missing in {from.TrimEnd(':')}");
    }

    // ---------- Analytics dimensions ----------

    [Test]
    public void Dimensions_WaitForConsent()
    {
        var consent = new ConsentService(new ConsentStore(new FakeStorageProvider()), new FakeClock());
        var backend = new FakeAnalyticsBackend();
        var analytics = new AnalyticsService(consent, backend);

        analytics.SetSegment(PlayerSegments.Stuck, PlayerSegments.VariantA, PlayerSegments.Early);
        analytics.Start();
        Assert.IsNull(backend.Dimensions, "Nothing reaches the provider without a yes");

        consent.SetAll(true);
        Assert.AreEqual("stuck/a/early", backend.Dimensions, "Set right after the provider starts");

        analytics.Stop();
    }

    [Test]
    public void Dimensions_SetWhileRunning_GoStraightToTheProvider()
    {
        var consent = new ConsentService(new ConsentStore(new FakeStorageProvider()), new FakeClock());
        consent.SetAll(true);
        var backend = new FakeAnalyticsBackend();
        var analytics = new AnalyticsService(consent, backend);
        analytics.Start();

        analytics.SetSegment(PlayerSegments.Loop, PlayerSegments.NoExperiment, PlayerSegments.Mature);

        Assert.AreEqual("loop/none/mature", backend.Dimensions);
        analytics.Stop();
    }
}
