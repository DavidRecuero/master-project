using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Saved privacy choices on the device: answers per purpose, version and date of the decision.</summary>
public class ConsentStoreTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Empty_HasNoAnswerNoVersionNoDate()
    {
        var store = new ConsentStore(new FakeStorageProvider());

        Assert.AreEqual(ConsentStatus.Unknown, store.GetStatus(ConsentPurpose.Analytics));
        Assert.AreEqual(0, store.Version);
        Assert.IsNull(store.DecisionUtc);
    }

    [Test]
    public void Save_IsReadBack()
    {
        var storage = new FakeStorageProvider();
        new ConsentStore(storage).Save(ConsentPurpose.Analytics, ConsentStatus.Granted, 3, Now);

        var store = new ConsentStore(storage);   // a new launch reads the same device data
        Assert.AreEqual(ConsentStatus.Granted, store.GetStatus(ConsentPurpose.Analytics));
        Assert.AreEqual(3, store.Version);
        Assert.AreEqual(Now, store.DecisionUtc);
    }

    [Test]
    public void StatusKey_IsReadable()
    {
        Assert.AreEqual("privacy.consent.analytics", ConsentStore.StatusKey(ConsentPurpose.Analytics));
    }

    [TestCase("")]
    [TestCase("yes")]
    [TestCase("0")]
    [TestCase("7")]
    public void UnreadableAnswer_CountsAsUnknown(string raw)
    {
        var storage = new FakeStorageProvider();
        storage.SetString(ConsentStore.StatusKey(ConsentPurpose.Analytics), raw);

        Assert.AreEqual(ConsentStatus.Unknown, new ConsentStore(storage).GetStatus(ConsentPurpose.Analytics));
    }

    [Test]
    public void UnreadableVersionAndDate_AreIgnored()
    {
        var storage = new FakeStorageProvider();
        storage.SetString(ConsentStore.VersionKey, "-4");
        storage.SetString(ConsentStore.DecisionKey, "not a date");
        var store = new ConsentStore(storage);

        Assert.AreEqual(0, store.Version);
        Assert.IsNull(store.DecisionUtc);
    }

    [Test]
    public void Clear_ForgetsEverything()
    {
        var store = new ConsentStore(new FakeStorageProvider());
        store.Save(ConsentPurpose.Analytics, ConsentStatus.Denied, 1, Now);

        store.Clear();

        Assert.AreEqual(ConsentStatus.Unknown, store.GetStatus(ConsentPurpose.Analytics));
        Assert.AreEqual(0, store.Version);
        Assert.IsNull(store.DecisionUtc);
    }

    [Test]
    public void NoStorage_DoesNothing()
    {
        var store = new ConsentStore(null);

        Assert.DoesNotThrow(() => store.Save(ConsentPurpose.Analytics, ConsentStatus.Granted, 1, Now));
        Assert.DoesNotThrow(store.Clear);
        Assert.AreEqual(ConsentStatus.Unknown, store.GetStatus(ConsentPurpose.Analytics));
    }
}

/// <summary>What the game is allowed to do with the player's data, and when the consent popup must show.</summary>
public class ConsentServiceTests
{
    private FakeStorageProvider _storage;
    private FakeClock _clock;
    private readonly List<(ConsentPurpose purpose, bool granted)> _changes = new List<(ConsentPurpose, bool)>();

    [SetUp]
    public void SetUp()
    {
        _storage = new FakeStorageProvider();
        _clock = new FakeClock();
        _changes.Clear();
        GameEvents.OnConsentChanged += OnChanged;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.OnConsentChanged -= OnChanged;
    }

    private void OnChanged(ConsentPurpose purpose, bool granted) => _changes.Add((purpose, granted));

    private ConsentService Create(int version = 1) => new ConsentService(new ConsentStore(_storage), _clock, version);

    // ---------- First launch ----------

    [Test]
    public void FirstLaunch_AsksAndAllowsNothing()
    {
        var consent = Create();

        Assert.IsTrue(consent.NeedsPrompt);
        Assert.AreEqual(ConsentStatus.Unknown, consent.GetStatus(ConsentPurpose.Analytics));
        Assert.IsFalse(consent.IsGranted(ConsentPurpose.Analytics));
        Assert.IsNull(consent.DecisionUtc);
    }

    [Test]
    public void Accept_AllowsEverythingAndStopsAsking()
    {
        var consent = Create();

        consent.SetAll(true);

        Assert.IsFalse(consent.NeedsPrompt);
        Assert.IsTrue(consent.IsGranted(ConsentPurpose.Analytics));
        Assert.AreEqual(_clock.UtcNow, consent.DecisionUtc);
    }

    [Test]
    public void Reject_AllowsNothingAndStopsAsking()
    {
        var consent = Create();

        consent.SetAll(false);

        Assert.IsFalse(consent.NeedsPrompt);
        Assert.AreEqual(ConsentStatus.Denied, consent.GetStatus(ConsentPurpose.Analytics));
        Assert.IsFalse(consent.IsGranted(ConsentPurpose.Analytics));
    }

    [Test]
    public void Answer_IsKeptBetweenLaunches()
    {
        Create().SetAll(true);

        var nextLaunch = Create();
        Assert.IsFalse(nextLaunch.NeedsPrompt);
        Assert.IsTrue(nextLaunch.IsGranted(ConsentPurpose.Analytics));
    }

    // ---------- Changing it later (Settings) ----------

    [Test]
    public void Withdraw_StopsAllowingAndUpdatesTheDate()
    {
        var consent = Create();
        consent.SetAll(true);
        _clock.Advance(TimeSpan.FromDays(3));

        consent.Set(ConsentPurpose.Analytics, false);

        Assert.IsFalse(consent.IsGranted(ConsentPurpose.Analytics));
        Assert.IsFalse(consent.NeedsPrompt);
        Assert.AreEqual(_clock.UtcNow, consent.DecisionUtc);
    }

    // ---------- Consent version ----------

    [Test]
    public void NewVersion_AsksAgainAndAllowsNothingUntilAnswered()
    {
        Create(version: 1).SetAll(true);

        var consent = Create(version: 2);

        Assert.IsTrue(consent.NeedsPrompt);
        Assert.AreEqual(ConsentStatus.Unknown, consent.GetStatus(ConsentPurpose.Analytics));
        Assert.IsFalse(consent.IsGranted(ConsentPurpose.Analytics));
    }

    [Test]
    public void NewVersion_AnsweredAgain_StopsAsking()
    {
        Create(version: 1).SetAll(false);
        var consent = Create(version: 2);

        consent.SetAll(true);

        Assert.IsFalse(consent.NeedsPrompt);
        Assert.IsTrue(consent.IsGranted(ConsentPurpose.Analytics));
    }

    [Test]
    public void OlderVersion_DoesNotAskAgain()
    {
        // A newer build answered, then an older build is installed again (e.g. a tester going back)
        Create(version: 2).SetAll(true);

        Assert.IsFalse(Create(version: 1).NeedsPrompt);
    }

    [Test]
    public void CurrentVersion_IsAtLeastOne()
    {
        Assert.GreaterOrEqual(ConsentService.CurrentVersion, 1);
    }

    // ---------- Event ----------

    [Test]
    public void Accept_RaisesTheChange()
    {
        Create().SetAll(true);

        CollectionAssert.AreEqual(new[] { (ConsentPurpose.Analytics, true) }, _changes);
    }

    [Test]
    public void Reject_OnFirstLaunch_RaisesNothing()
    {
        // Nothing was running, so there is nothing to stop
        Create().SetAll(false);

        Assert.IsEmpty(_changes);
    }

    [Test]
    public void SameAnswerTwice_RaisesOnlyOnce()
    {
        var consent = Create();
        consent.SetAll(true);
        consent.Set(ConsentPurpose.Analytics, true);

        Assert.AreEqual(1, _changes.Count);
    }

    [Test]
    public void Withdraw_RaisesTheChange()
    {
        var consent = Create();
        consent.SetAll(true);
        _changes.Clear();

        consent.Set(ConsentPurpose.Analytics, false);

        CollectionAssert.AreEqual(new[] { (ConsentPurpose.Analytics, false) }, _changes);
    }

    // ---------- Reset (debug) ----------

    [Test]
    public void Reset_AsksAgainAndStopsWhatWasAllowed()
    {
        var consent = Create();
        consent.SetAll(true);
        _changes.Clear();

        consent.Reset();

        Assert.IsTrue(consent.NeedsPrompt);
        Assert.IsFalse(consent.IsGranted(ConsentPurpose.Analytics));
        Assert.IsNull(consent.DecisionUtc);
        CollectionAssert.AreEqual(new[] { (ConsentPurpose.Analytics, false) }, _changes);
    }

    [Test]
    public void Reset_AfterReject_RaisesNothing()
    {
        var consent = Create();
        consent.SetAll(false);

        consent.Reset();

        Assert.IsTrue(consent.NeedsPrompt);
        Assert.IsEmpty(_changes);
    }

    // ---------- Shared instance ----------

    [Test]
    public void SharedService_CanBeReplacedAndRestored()
    {
        var fake = Create();
        try
        {
            ConsentServices.Set(fake);
            Assert.AreSame(fake, ConsentServices.Current);
        }
        finally
        {
            ConsentServices.Set(null);
        }

        Assert.IsNotNull(ConsentServices.Current);
        Assert.AreNotSame(fake, ConsentServices.Current);
    }
}
