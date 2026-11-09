using System;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The lives rules on a real profile with a fake clock: use, give back, regenerate (also "with the game
/// closed", i.e. only by moving the clock), buy, refill, and the edge cases of saved data and device time.
/// </summary>
public class LivesServiceTests
{
    private static readonly LivesRules Rules = new LivesRules(5, 30 * 60, 20);

    private UserProfile _profile;
    private FakeUserDataProvider _wallet;
    private FakeClock _clock;
    private int _saves;
    private int _changes;
    private LivesService _lives;

    [SetUp]
    public void SetUp()
    {
        _profile = new UserProfile();
        _wallet = new FakeUserDataProvider { Coins = 100 };
        _clock = new FakeClock();
        _saves = 0;
        _changes = 0;
        _lives = Create(_profile);
    }

    private LivesService Create(UserProfile profile)
    {
        var lives = new LivesService(Rules, () => profile, _wallet, () => _saves++, _clock);
        lives.LivesChanged += () => _changes++;
        return lives;
    }

    private void UseLives(int count)
    {
        for (int i = 0; i < count; i++) Assert.IsTrue(_lives.TryUseLife());
    }

    // ---------- Start ----------

    [Test]
    public void NewProfile_IsFull()
    {
        Assert.AreEqual(5, _lives.Lives);
        Assert.IsTrue(_lives.IsFull);
        Assert.IsTrue(_lives.HasLives);
        Assert.AreEqual(TimeSpan.Zero, _lives.TimeToNextLife);
        Assert.AreEqual(0, _lives.RefillPrice);
    }

    [Test]
    public void ProfileSavedBeforeTheLivesExisted_LoadsFull()
    {
        UserProfile old = JsonUtility.FromJson<UserProfile>("{\"UserId\":\"a\",\"CurrentLevel\":7,\"Coins\":40}");
        LivesService lives = Create(old);

        Assert.AreEqual(5, lives.Lives);
        Assert.IsTrue(lives.IsFull);
    }

    [Test]
    public void NoProfile_ActsFullAndChangesNothing()
    {
        var lives = new LivesService(Rules, () => null, _wallet, () => _saves++, _clock);

        Assert.AreEqual(5, lives.Lives);
        Assert.IsFalse(lives.TryUseLife());
        Assert.AreEqual(0, _saves);
    }

    // ---------- Use and give back ----------

    [Test]
    public void TryUseLife_TakesOneSavesAndStartsTheCountdown()
    {
        Assert.IsTrue(_lives.TryUseLife());

        Assert.AreEqual(4, _lives.Lives);
        Assert.AreEqual(1, _saves);
        Assert.AreEqual(1, _changes);
        Assert.AreEqual(TimeSpan.FromMinutes(30), _lives.TimeToNextLife);
    }

    [Test]
    public void TryUseLife_WithNoLives_ReturnsFalseAndChangesNothing()
    {
        UseLives(5);
        int saves = _saves;

        Assert.IsFalse(_lives.TryUseLife());
        Assert.AreEqual(0, _lives.Lives);
        Assert.IsFalse(_lives.HasLives);
        Assert.AreEqual(saves, _saves);
    }

    [Test]
    public void SecondLifeUsed_DoesNotRestartTheCountdown()
    {
        _lives.TryUseLife();
        _clock.AdvanceMinutes(10);

        _lives.TryUseLife();

        Assert.AreEqual(TimeSpan.FromMinutes(20), _lives.TimeToNextLife, "The first missing life keeps its countdown");
    }

    [Test]
    public void ReturnLife_GivesItBack_AndFullStopsTheCountdown()
    {
        _lives.TryUseLife();
        _clock.AdvanceMinutes(5);

        _lives.ReturnLife();

        Assert.AreEqual(5, _lives.Lives);
        Assert.AreEqual(0, _profile.NextLifeStartTicks);
        Assert.AreEqual(TimeSpan.Zero, _lives.TimeToNextLife);
    }

    [Test]
    public void ReturnLife_WhenFull_DoesNothing()
    {
        _lives.ReturnLife();

        Assert.AreEqual(5, _lives.Lives);
        Assert.AreEqual(0, _saves);
    }

    // ---------- Regeneration ----------

    [Test]
    public void OneLifeComesBack_AfterThirtyMinutes()
    {
        UseLives(2);

        _clock.AdvanceMinutes(29.9);
        Assert.AreEqual(3, _lives.Lives);

        _clock.AdvanceMinutes(0.1);
        Assert.AreEqual(4, _lives.Lives);
        Assert.AreEqual(TimeSpan.FromMinutes(30), _lives.TimeToNextLife, "The next one starts right away");
    }

    [Test]
    public void LivesComeBack_WithTheGameClosed()
    {
        UseLives(5);

        // "Closing" the game: a new service on the same saved profile, much later
        _clock.AdvanceMinutes(95);
        LivesService reopened = Create(_profile);

        Assert.AreEqual(3, reopened.Lives, "95 minutes = 3 lives");
        Assert.AreEqual(TimeSpan.FromMinutes(25), reopened.TimeToNextLife, "The 5 extra minutes count for the next one");
    }

    [Test]
    public void Regeneration_NeverGoesOverTheMaximum()
    {
        UseLives(3);

        _clock.Advance(TimeSpan.FromDays(3));

        Assert.AreEqual(5, _lives.Lives);
        Assert.IsTrue(_lives.IsFull);
        Assert.AreEqual(0, _profile.NextLifeStartTicks);
    }

    [Test]
    public void Regeneration_DoesNotSave()
    {
        UseLives(2);
        int saves = _saves;

        _clock.AdvanceMinutes(60);
        _ = _lives.Lives;

        Assert.AreEqual(saves, _saves, "Worked out on read; saved with the next real change");
    }

    [Test]
    public void ClockMovedBack_RestartsTheCountdown_WithoutBreaking()
    {
        UseLives(2);
        _clock.AdvanceMinutes(-120);

        Assert.AreEqual(3, _lives.Lives, "No life is given or taken");
        Assert.AreEqual(TimeSpan.FromMinutes(30), _lives.TimeToNextLife, "Never more than one full countdown");
    }

    [Test]
    public void SavedValuesOutOfRange_AreFixed()
    {
        _profile.MissingLives = 9;
        Assert.AreEqual(0, _lives.Lives);

        _profile.MissingLives = -3;
        Assert.AreEqual(5, _lives.Lives);
    }

    [Test]
    public void MissingLivesWithoutAStartTime_StartTheCountdownNow()
    {
        _profile.MissingLives = 2;
        _profile.NextLifeStartTicks = 0;

        Assert.AreEqual(3, _lives.Lives);
        Assert.AreEqual(TimeSpan.FromMinutes(30), _lives.TimeToNextLife);
    }

    // ---------- Buying ----------

    [Test]
    public void TryBuyLife_PaysAndGivesOne()
    {
        UseLives(5);

        Assert.IsTrue(_lives.TryBuyLife());

        Assert.AreEqual(1, _lives.Lives);
        Assert.AreEqual(80, _wallet.Coins);
    }

    [Test]
    public void TryBuyLife_KeepsTheCountdownOfTheNextOne()
    {
        UseLives(5);
        _clock.AdvanceMinutes(10);

        _lives.TryBuyLife();

        Assert.AreEqual(TimeSpan.FromMinutes(20), _lives.TimeToNextLife);
    }

    [Test]
    public void TryBuyLife_WithoutEnoughCoins_SpendsNothing()
    {
        UseLives(5);
        _wallet.Coins = 19;

        Assert.IsFalse(_lives.TryBuyLife());
        Assert.AreEqual(0, _lives.Lives);
        Assert.AreEqual(19, _wallet.Coins);
    }

    [Test]
    public void TryBuyLife_WhenFull_SpendsNothing()
    {
        Assert.IsFalse(_lives.TryBuyLife());
        Assert.AreEqual(100, _wallet.Coins);
    }

    [Test]
    public void RefillPrice_IsOneLifePriceTimesTheMissingLives()
    {
        UseLives(3);

        Assert.AreEqual(60, _lives.RefillPrice);
    }

    [Test]
    public void TryRefill_PaysForEveryMissingLife()
    {
        UseLives(3);

        Assert.IsTrue(_lives.TryRefill());

        Assert.AreEqual(5, _lives.Lives);
        Assert.AreEqual(40, _wallet.Coins);
        Assert.AreEqual(0, _profile.NextLifeStartTicks);
    }

    [Test]
    public void TryRefill_UsesTheLivesThatAlreadyCameBack()
    {
        UseLives(3);
        _clock.AdvanceMinutes(30);   // one came back: only 2 to pay

        Assert.AreEqual(40, _lives.RefillPrice);
        Assert.IsTrue(_lives.TryRefill());
        Assert.AreEqual(60, _wallet.Coins);
    }

    [Test]
    public void TryRefill_WithoutEnoughCoins_SpendsNothing()
    {
        UseLives(5);   // 100 coins needed

        _wallet.Coins = 99;

        Assert.IsFalse(_lives.TryRefill());
        Assert.AreEqual(0, _lives.Lives);
        Assert.AreEqual(99, _wallet.Coins);
    }

    [Test]
    public void CanAfford_ComparesWithTheCoins()
    {
        Assert.IsTrue(_lives.CanAfford(100));
        Assert.IsFalse(_lives.CanAfford(101));
    }

    // ---------- Profile replaced ----------

    [Test]
    public void NotifyChanged_TellsTheListeners()
    {
        _lives.NotifyChanged();

        Assert.AreEqual(1, _changes);
    }

    [Test]
    public void ReplacedProfile_IsReadFromThen()
    {
        UserProfile current = _profile;
        var lives = new LivesService(Rules, () => current, _wallet, null, _clock);
        lives.TryUseLife();

        current = new UserProfile();   // e.g. the cloud copy won

        Assert.AreEqual(5, lives.Lives);
    }

    [Test]
    public void Rules_AreKeptInRange()
    {
        var rules = new LivesRules(0, 0, -5);

        Assert.AreEqual(1, rules.MaxLives);
        Assert.AreEqual(1, rules.SecondsPerLife);
        Assert.AreEqual(0, rules.CoinsPerLife);
    }
}
