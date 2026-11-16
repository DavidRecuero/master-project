using NUnit.Framework;

/// <summary>New lives numbers from Remote Config, applied over a profile that already has lives in use.</summary>
public class LivesRemoteRulesTests
{
    private UserProfile _profile;
    private FakeClock _clock;
    private LivesService _lives;
    private int _changes;

    [SetUp]
    public void SetUp()
    {
        _profile = new UserProfile();
        _clock = new FakeClock();
        _lives = new LivesService(new LivesRules(5, 30 * 60, 20), () => _profile,
            new FakeUserDataProvider { Coins = 100 }, () => { }, _clock);
        _lives.LivesChanged += () => _changes++;
    }

    [Test]
    public void SetRules_ChangesTheNumbers_AndTellsTheListeners()
    {
        _lives.SetRules(new LivesRules(8, 10 * 60, 5));

        Assert.AreEqual(8, _lives.MaxLives);
        Assert.AreEqual(5, _lives.LifePrice);
        Assert.AreEqual(8, _lives.Lives, "A full player stays full with a bigger maximum");
        Assert.AreEqual(1, _changes);
    }

    [Test]
    public void SmallerMaximum_CapsTheMissingLives()
    {
        for (int i = 0; i < 5; i++) _lives.TryUseLife();

        _lives.SetRules(new LivesRules(3, 30 * 60, 20));

        Assert.AreEqual(0, _lives.Lives);
        Assert.AreEqual(3, _lives.MaxLives);
    }

    [Test]
    public void ShorterTimePerLife_AppliesToTheCountdownInProgress()
    {
        _lives.TryUseLife();
        _clock.AdvanceMinutes(12);

        _lives.SetRules(new LivesRules(5, 10 * 60, 20));

        Assert.IsTrue(_lives.IsFull, "12 minutes already passed and a life now takes 10");
    }
}
