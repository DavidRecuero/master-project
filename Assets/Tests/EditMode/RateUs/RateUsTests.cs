using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Records the card requests and answers with the result the test chose (now, or later to mimic Google).</summary>
public class FakeReviewPrompter : IReviewPrompter
{
    public ReviewRequestResult Result = ReviewRequestResult.Launched;
    public bool AnswerLater;
    public int Prepares, Requests, Cancels;
    private readonly List<Action<ReviewRequestResult>> _pending = new List<Action<ReviewRequestResult>>();

    public void Prepare() => Prepares++;

    public void RequestReview(Action<ReviewRequestResult> onDone)
    {
        Requests++;
        if (AnswerLater) _pending.Add(onDone);
        else onDone?.Invoke(Result);
    }

    public void Cancel()
    {
        Cancels++;
        foreach (var done in _pending) done?.Invoke(ReviewRequestResult.Failed);
        _pending.Clear();
    }

    public void AnswerPending()
    {
        foreach (var done in _pending) done?.Invoke(Result);
        _pending.Clear();
    }
}

/// <summary>When the review card is asked for: first level, maximum attempts, days between attempts.</summary>
public class RateUsPolicyTests
{
    private static readonly RateUsRules Rules = new RateUsRules(8, 3, 30, 1f);
    private static readonly DateTime Now = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

    [TestCase(1, false)]
    [TestCase(7, false)]
    [TestCase(8, true)]
    [TestCase(40, true)]
    public void FirstTime_FromTheFirstPromptLevel(int level, bool expected)
    {
        Assert.AreEqual(expected, RateUsPolicy.ShouldAsk(level, 0, null, Now, Rules));
    }

    [Test]
    public void AfterTheFirstTime_WaitsTheDaysBetweenPrompts()
    {
        Assert.IsFalse(RateUsPolicy.ShouldAsk(12, 1, Now.AddDays(-29.9), Now, Rules));
        Assert.IsTrue(RateUsPolicy.ShouldAsk(12, 1, Now.AddDays(-30), Now, Rules));
    }

    [Test]
    public void NeverMoreThanTheMaximum()
    {
        Assert.IsFalse(RateUsPolicy.ShouldAsk(50, 3, Now.AddDays(-365), Now, Rules));
    }

    [Test]
    public void ClockMovedBack_Waits()
    {
        Assert.IsFalse(RateUsPolicy.ShouldAsk(12, 1, Now.AddDays(5), Now, Rules));
    }

    [Test]
    public void ZeroMaximum_TurnsItOff()
    {
        Assert.IsFalse(RateUsPolicy.ShouldAsk(20, 0, null, Now, new RateUsRules(8, 0, 30, 1f)));
    }

    [Test]
    public void Rules_AreKeptInRange()
    {
        var rules = new RateUsRules(0, -1, -5, -2f);

        Assert.AreEqual(1, rules.FirstPromptLevel);
        Assert.AreEqual(0, rules.MaxPrompts);
        Assert.AreEqual(0, rules.DaysBetweenPrompts);
        Assert.AreEqual(0f, rules.DelayAfterWinSeconds);
    }
}

public class RateUsStoreTests
{
    [Test]
    public void Empty_HasNoPrompts()
    {
        var store = new RateUsStore(new FakeStorageProvider());

        Assert.AreEqual(0, store.Prompts);
        Assert.IsNull(store.LastPromptUtc);
    }

    [Test]
    public void RecordPrompt_CountsAndKeepsTheTime()
    {
        var storage = new FakeStorageProvider();
        var time = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

        new RateUsStore(storage).RecordPrompt(time);
        new RateUsStore(storage).RecordPrompt(time.AddDays(31));
        var reopened = new RateUsStore(storage);

        Assert.AreEqual(2, reopened.Prompts);
        Assert.AreEqual(time.AddDays(31), reopened.LastPromptUtc);
    }

    [Test]
    public void BrokenValues_ReadAsNothing()
    {
        var storage = new FakeStorageProvider();
        storage.SetString(RateUsStore.PromptsKey, "abc");
        storage.SetString(RateUsStore.LastPromptKey, "-5");

        var store = new RateUsStore(storage);

        Assert.AreEqual(0, store.Prompts);
        Assert.IsNull(store.LastPromptUtc);
    }
}

public class RateUsServiceTests
{
    private FakeStorageProvider _storage;
    private RateUsStore _store;
    private FakeReviewPrompter _prompter;
    private FakeClock _clock;
    private RateUsService _service;

    [SetUp]
    public void SetUp()
    {
        _storage = new FakeStorageProvider();
        _store = new RateUsStore(_storage);
        _prompter = new FakeReviewPrompter();
        _clock = new FakeClock();
        _service = new RateUsService(new RateUsRules(8, 3, 30, 1f), _store, _prompter, _clock);
    }

    [Test]
    public void BeforeTheFirstLevel_AsksNothing()
    {
        Assert.IsFalse(_service.TryPrompt(7));
        Assert.AreEqual(0, _prompter.Requests);
    }

    [Test]
    public void FirstPromptLevel_AsksAndRemembers()
    {
        Assert.IsTrue(_service.TryPrompt(8));

        Assert.AreEqual(1, _prompter.Requests);
        Assert.AreEqual(1, _store.Prompts);
        Assert.AreEqual(_clock.UtcNow, _store.LastPromptUtc);
    }

    [Test]
    public void NextWins_WaitThirtyDays()
    {
        _service.TryPrompt(8);

        _clock.Advance(TimeSpan.FromDays(10));
        Assert.IsFalse(_service.TryPrompt(15));

        _clock.Advance(TimeSpan.FromDays(20));
        Assert.IsTrue(_service.TryPrompt(30));
        Assert.AreEqual(2, _store.Prompts);
    }

    [Test]
    public void AtMostThreeTimes()
    {
        for (int i = 0; i < 5; i++)
        {
            _service.TryPrompt(20 + i);
            _clock.Advance(TimeSpan.FromDays(31));
        }

        Assert.AreEqual(3, _prompter.Requests);
        Assert.AreEqual(3, _store.Prompts);
    }

    [Test]
    public void FailedRequest_IsNotAnAttempt_AndTheNextWinTriesAgain()
    {
        _prompter.Result = ReviewRequestResult.Failed;
        _service.TryPrompt(8);

        Assert.AreEqual(0, _store.Prompts);

        _prompter.Result = ReviewRequestResult.Launched;
        Assert.IsTrue(_service.TryPrompt(9));
        Assert.AreEqual(1, _store.Prompts);
    }

    [Test]
    public void Prepare_OnlyWhenTheCardWillBeAskedFor()
    {
        _service.Prepare(7);
        Assert.AreEqual(0, _prompter.Prepares);

        _service.Prepare(8);
        Assert.AreEqual(1, _prompter.Prepares);
    }

    [Test]
    public void Cancel_BeforeTheCardOpens_IsNotAnAttempt()
    {
        _prompter.AnswerLater = true;
        _service.TryPrompt(8);

        _service.Cancel();

        Assert.AreEqual(1, _prompter.Cancels);
        Assert.AreEqual(0, _store.Prompts);
        Assert.IsTrue(_service.ShouldAsk(9), "The next win asks again");
    }

    [Test]
    public void Cancel_WithNothingGoing_DoesNothing()
    {
        _service.Cancel();

        Assert.AreEqual(0, _prompter.Cancels);
    }

    [Test]
    public void WhileARequestIsGoing_DoesNotAskAgain()
    {
        _prompter.AnswerLater = true;

        _service.TryPrompt(8);
        Assert.IsFalse(_service.TryPrompt(9));

        _prompter.AnswerPending();
        Assert.AreEqual(1, _prompter.Requests);
        Assert.AreEqual(1, _store.Prompts);
    }
}

/// <summary>The Level scene trigger: only wins ask, at the win popup.</summary>
public class RateUsTriggerTests
{
    private GameObject _go;
    private RateUsTrigger _trigger;
    private FakeReviewPrompter _prompter;
    private int _level;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("RateUs");
        _trigger = _go.AddComponent<RateUsTrigger>();
        _prompter = new FakeReviewPrompter();
        var service = new RateUsService(new RateUsRules(8, 3, 30, 0f), new RateUsStore(new FakeStorageProvider()), _prompter, new FakeClock());
        _level = 8;
        _trigger.Initialize(service, () => _level);
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(_go);

    [Test]
    public void Win_AsksForTheCard()
    {
        _trigger.OnResultShown(true);

        Assert.AreEqual(1, _prompter.Requests);
    }

    [Test]
    public void Defeat_NeverAsks()
    {
        _trigger.OnResultShown(false);

        Assert.AreEqual(0, _prompter.Requests);
    }

    [Test]
    public void ForcedOrUnknownLevel_NeverAsks()
    {
        _level = 0;   // what the trigger reads for Tools > Level Loader levels

        _trigger.OnResultShown(true);

        Assert.AreEqual(0, _prompter.Requests);
    }

    [Test]
    public void LevelWon_GetsTheCardReady()
    {
        _trigger.OnLevelCleared();

        Assert.AreEqual(1, _prompter.Prepares);
    }

    [Test]
    public void LeavingTheResultBeforeTheCardOpens_DropsIt()
    {
        _prompter.AnswerLater = true;   // Google still preparing the card
        _trigger.OnResultShown(true);

        _trigger.LeaveResult();         // player taps "Next level"
        _prompter.AnswerPending();      // a late answer must not open anything

        Assert.AreEqual(1, _prompter.Cancels);
        Assert.AreEqual(1, _prompter.Requests);
    }

    [Test]
    public void LeavingWithNothingGoing_CancelsNothing()
    {
        _trigger.LeaveResult();

        Assert.AreEqual(0, _prompter.Cancels);
    }
}
