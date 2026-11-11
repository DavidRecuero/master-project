using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Records what the service asked the provider to do. Sends nothing.</summary>
public class FakeAnalyticsBackend : IAnalyticsBackend
{
    public bool IsInitialized { get; private set; }
    public int InitializeCalls;
    public readonly List<bool> SubmissionChanges = new List<bool>();
    public readonly List<string> Events = new List<string>();

    public void Initialize()
    {
        InitializeCalls++;
        IsInitialized = true;
    }

    /// <summary>Submission and session calls in order, e.g. "submission:False", "end", "start".</summary>
    public readonly List<string> Order = new List<string>();

    public void SetSubmissionEnabled(bool enabled)
    {
        SubmissionChanges.Add(enabled);
        Order.Add($"submission:{enabled}");
    }

    public int StartSessionCalls, EndSessionCalls;

    public void StartSession()
    {
        StartSessionCalls++;
        Order.Add("start");
    }

    public void EndSession()
    {
        EndSessionCalls++;
        Order.Add("end");
    }

    public void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null) =>
        Events.Add($"progression:{status}:{progression01}:{progression02}:{score}");

    public void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId) =>
        Events.Add($"resource:{flow}:{currency}:{amount}:{itemType}:{itemId}");

    public void Design(string eventId, float? value = null) => Events.Add($"design:{eventId}:{value}");
}

/// <summary>Analytics only with consent: start, stop sending, start again, and events dropped without a yes.</summary>
public class AnalyticsServiceTests
{
    private ConsentService _consent;
    private FakeAnalyticsBackend _backend;
    private AnalyticsService _analytics;

    [SetUp]
    public void SetUp()
    {
        _consent = new ConsentService(new ConsentStore(new FakeStorageProvider()), new FakeClock());
        _backend = new FakeAnalyticsBackend();
        _analytics = new AnalyticsService(_consent, _backend);
    }

    [TearDown]
    public void TearDown()
    {
        _analytics.Stop();
    }

    private void SendOneOfEach()
    {
        _analytics.Progression(AnalyticsProgressionStatus.Start, "Level_001");
        _analytics.Resource(AnalyticsResourceFlow.Source, "Coins", 20, "Reward", "LevelWin");
        _analytics.Design("Booster:Used:Shuffle");
    }

    // ---------- Start ----------

    [Test]
    public void Start_WithConsent_StartsTheProvider()
    {
        _consent.SetAll(true);

        _analytics.Start();

        Assert.AreEqual(1, _backend.InitializeCalls);
        Assert.IsTrue(_analytics.IsActive);
    }

    [Test]
    public void Start_WithoutAnswer_CreatesNothing()
    {
        _analytics.Start();

        Assert.AreEqual(0, _backend.InitializeCalls);
        Assert.IsFalse(_analytics.IsActive);
    }

    [Test]
    public void Start_AfterReject_CreatesNothing()
    {
        _consent.SetAll(false);

        _analytics.Start();

        Assert.AreEqual(0, _backend.InitializeCalls);
        Assert.IsEmpty(_backend.SubmissionChanges);
    }

    [Test]
    public void Start_Twice_StartsOnce()
    {
        _consent.SetAll(true);

        _analytics.Start();
        _analytics.Start();

        Assert.AreEqual(1, _backend.InitializeCalls);
    }

    // ---------- Changing the consent later (Settings) ----------

    [Test]
    public void YesLater_StartsTheProviderThen()
    {
        _consent.SetAll(false);
        _analytics.Start();

        _consent.Set(ConsentPurpose.Analytics, true);

        Assert.AreEqual(1, _backend.InitializeCalls);
        Assert.IsTrue(_analytics.IsActive);
    }

    [Test]
    public void Withdraw_StopsSending()
    {
        _consent.SetAll(true);
        _analytics.Start();

        _consent.Set(ConsentPurpose.Analytics, false);

        CollectionAssert.AreEqual(new[] { false }, _backend.SubmissionChanges);
        Assert.IsFalse(_analytics.IsActive);
    }

    [Test]
    public void YesAgain_SendsAgainWithoutStartingTwice()
    {
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);

        _consent.Set(ConsentPurpose.Analytics, true);

        Assert.AreEqual(1, _backend.InitializeCalls);
        CollectionAssert.AreEqual(new[] { false, true }, _backend.SubmissionChanges);
        Assert.IsTrue(_analytics.IsActive);
    }

    [Test]
    public void AfterStop_ConsentChangesAreIgnored()
    {
        _consent.SetAll(false);
        _analytics.Start();
        _analytics.Stop();

        _consent.Set(ConsentPurpose.Analytics, true);

        Assert.AreEqual(0, _backend.InitializeCalls);
    }

    // ---------- Sessions (background / foreground) ----------

    [Test]
    public void Withdraw_EndsTheSession()
    {
        _consent.SetAll(true);
        _analytics.Start();

        _consent.Set(ConsentPurpose.Analytics, false);

        Assert.AreEqual(1, _backend.EndSessionCalls);
    }

    [Test]
    public void Withdraw_TurnsSendingOffBeforeEndingTheSession()
    {
        // Otherwise the end of the session would still be sent after the player said no
        _consent.SetAll(true);
        _analytics.Start();

        _consent.Set(ConsentPurpose.Analytics, false);

        CollectionAssert.AreEqual(new[] { "submission:False", "end" }, _backend.Order);
    }

    [Test]
    public void YesAgain_TurnsSendingOnBeforeStartingTheSession()
    {
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);
        _backend.Order.Clear();

        _consent.Set(ConsentPurpose.Analytics, true);

        CollectionAssert.AreEqual(new[] { "submission:True", "start" }, _backend.Order);
    }

    [Test]
    public void YesAgain_StartsANewSession()
    {
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);

        _consent.Set(ConsentPurpose.Analytics, true);

        Assert.AreEqual(1, _backend.StartSessionCalls);
    }

    [Test]
    public void Background_EndsTheSession_AndForeground_StartsANewOne()
    {
        _consent.SetAll(true);
        _analytics.Start();

        _analytics.SetAppPaused(true);
        _analytics.SetAppPaused(false);

        Assert.AreEqual(1, _backend.EndSessionCalls);
        Assert.AreEqual(1, _backend.StartSessionCalls);
    }

    [Test]
    public void ForegroundAfterWithdrawing_StartsNoSession()
    {
        // The bug this guards against: the provider contacted its server on every return to the foreground
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);

        _analytics.SetAppPaused(true);
        _analytics.SetAppPaused(false);

        Assert.AreEqual(0, _backend.StartSessionCalls);
        Assert.AreEqual(1, _backend.EndSessionCalls, "Only the end when the consent was withdrawn");
    }

    [Test]
    public void YesWhileInTheBackground_WaitsForTheForeground()
    {
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);
        _analytics.SetAppPaused(true);

        _consent.Set(ConsentPurpose.Analytics, true);
        Assert.AreEqual(0, _backend.StartSessionCalls);

        _analytics.SetAppPaused(false);
        Assert.AreEqual(1, _backend.StartSessionCalls);
    }

    [Test]
    public void PauseWithoutConsent_TouchesNothing()
    {
        _analytics.Start();

        _analytics.SetAppPaused(true);
        _analytics.SetAppPaused(false);

        Assert.AreEqual(0, _backend.InitializeCalls);
        Assert.AreEqual(0, _backend.StartSessionCalls);
        Assert.AreEqual(0, _backend.EndSessionCalls);
    }

    [Test]
    public void RepeatedPause_CountsOnce()
    {
        _consent.SetAll(true);
        _analytics.Start();

        _analytics.SetAppPaused(true);
        _analytics.SetAppPaused(true);
        _analytics.SetAppPaused(false);
        _analytics.SetAppPaused(false);

        Assert.AreEqual(1, _backend.EndSessionCalls);
        Assert.AreEqual(1, _backend.StartSessionCalls);
    }

    // ---------- Events ----------

    [Test]
    public void Events_WithConsent_AreSent()
    {
        _consent.SetAll(true);
        _analytics.Start();

        SendOneOfEach();

        CollectionAssert.AreEqual(new[]
        {
            "progression:Start:Level_001::",
            "resource:Source:Coins:20:Reward:LevelWin",
            "design:Booster:Used:Shuffle:",
        }, _backend.Events);
    }

    [Test]
    public void Events_WithoutConsent_AreDropped()
    {
        _analytics.Start();

        SendOneOfEach();

        Assert.IsEmpty(_backend.Events);
    }

    [Test]
    public void Events_AfterWithdrawing_AreDropped()
    {
        _consent.SetAll(true);
        _analytics.Start();
        _consent.Set(ConsentPurpose.Analytics, false);

        SendOneOfEach();

        Assert.IsEmpty(_backend.Events);
    }

    [Test]
    public void Events_BeforeStart_AreDropped()
    {
        _consent.SetAll(true);

        SendOneOfEach();

        Assert.IsEmpty(_backend.Events);
    }

    [Test]
    public void InvalidEvents_AreDropped()
    {
        _consent.SetAll(true);
        _analytics.Start();

        _analytics.Progression(AnalyticsProgressionStatus.Start, "");
        _analytics.Resource(AnalyticsResourceFlow.Sink, "Coins", 0, "Booster", "Shuffle");
        _analytics.Resource(AnalyticsResourceFlow.Sink, "", 10, "Booster", "Shuffle");
        _analytics.Design(null);

        Assert.IsEmpty(_backend.Events);
    }

    // ---------- Shared instance ----------

    [Test]
    public void SharedService_IsSilentUntilTheBootSetsIt()
    {
        AnalyticsServices.Set(null);

        Assert.IsNotNull(AnalyticsServices.Current);
        Assert.IsFalse(AnalyticsServices.Current.IsActive);
        Assert.DoesNotThrow(() => AnalyticsServices.Current.Design("Test:Event"));
    }

    [Test]
    public void SharedService_CanBeReplaced()
    {
        try
        {
            AnalyticsServices.Set(_analytics);
            Assert.AreSame(_analytics, AnalyticsServices.Current);
        }
        finally
        {
            AnalyticsServices.Set(null);
        }
    }
}
