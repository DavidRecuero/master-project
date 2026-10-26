using NUnit.Framework;

public class GameplayAudioRouterTests
{
    private FakeAudioService _audio;
    private GameplayAudioRouter _router;
    private float _now;
    private int _freeSlots;

    [SetUp]
    public void SetUp()
    {
        _audio = new FakeAudioService();
        _now = 0f;
        _freeSlots = 7;
        _router = new GameplayAudioRouter(_audio, () => _now, () => _freeSlots, startingCoins: 100,
                                          combo: new ComboTracker(window: 2f, maxSteps: 4, semitonesPerStep: 2f));
        _router.Subscribe();
    }

    [TearDown]
    public void TearDown()
    {
        _router.Unsubscribe();
    }

    [Test]
    public void PlayerTap_PlaysItemTap()
    {
        GameEvents.TriggerItemCollectedByPlayer(null, null);
        Assert.AreEqual(SfxId.ItemTap, _audio.Last);
    }

    [Test]
    public void ItemLanding_PlaysItemLand()
    {
        GameEvents.TriggerItemLandedInTray(null);
        Assert.AreEqual(SfxId.ItemLand, _audio.Last);
    }

    [Test]
    public void Match_FirstOneAtNormalPitch()
    {
        GameEvents.TriggerMatchCompleted(0);

        Assert.AreEqual(SfxId.Match3, _audio.Last);
        Assert.AreEqual(1f, _audio.Sfx[0].pitch, 0.0001f);
    }

    [Test]
    public void Match_InQuickSuccession_GoesUpInPitch()
    {
        GameEvents.TriggerMatchCompleted(0);
        _now = 1f;
        GameEvents.TriggerMatchCompleted(1);

        Assert.Greater(_audio.Sfx[1].pitch, _audio.Sfx[0].pitch);
    }

    [Test]
    public void Match_AfterTheComboWindow_StartsOver()
    {
        GameEvents.TriggerMatchCompleted(0);
        _now = 1f;
        GameEvents.TriggerMatchCompleted(1);
        _now = 10f;
        GameEvents.TriggerMatchCompleted(2);

        Assert.AreEqual(1f, _audio.Sfx[2].pitch, 0.0001f);
    }

    [Test]
    public void LevelCleared_PlaysWinAndDucksMusic()
    {
        GameEvents.TriggerLevelCleared();

        Assert.AreEqual(SfxId.LevelWin, _audio.Last);
        Assert.Greater(_audio.LastDuckSeconds, 0f);
    }

    [Test]
    public void LevelFailed_PlaysLose()
    {
        GameEvents.TriggerLevelFailed();
        Assert.AreEqual(SfxId.LevelLose, _audio.Last);
    }

    [Test]
    public void LevelEnd_OnlyTheFirstResultPlays()
    {
        GameEvents.TriggerLevelFailed();
        GameEvents.TriggerLevelCleared();

        Assert.AreEqual(1, _audio.Count(SfxId.LevelLose));
        Assert.AreEqual(0, _audio.Count(SfxId.LevelWin));
    }

    [Test]
    public void AfterLevelEnd_TapsAndFogAreSilent()
    {
        GameEvents.TriggerLevelCleared();
        GameEvents.TriggerItemCollectedByPlayer(null, null);
        GameEvents.TriggerFogMoved();

        Assert.AreEqual(0, _audio.Count(SfxId.ItemTap));
        Assert.AreEqual(0, _audio.Count(SfxId.FogMove));
    }

    [TestCase(BoosterType.Match, SfxId.BoosterMatch)]
    [TestCase(BoosterType.Shuffle, SfxId.BoosterShuffle)]
    [TestCase(BoosterType.TrayClearer, SfxId.BoosterTrayClear)]
    public void BoosterUsed_PlaysItsOwnSound(BoosterType type, SfxId expected)
    {
        GameEvents.TriggerBoosterUsed(type);
        Assert.AreEqual(expected, _audio.Last);
    }

    [Test]
    public void BoosterDenied_PlaysDenied()
    {
        GameEvents.TriggerBoosterDenied(BoosterType.Shuffle);
        Assert.AreEqual(SfxId.BoosterDenied, _audio.Last);
    }

    [Test]
    public void BarrelDamaged_HitWhileAlive_BreakAtZero()
    {
        GameEvents.TriggerBarrelDamaged(null, 2);
        Assert.AreEqual(SfxId.BarrelHit, _audio.Last);

        GameEvents.TriggerBarrelDamaged(null, 0);
        Assert.AreEqual(SfxId.BarrelBreak, _audio.Last);
    }

    [Test]
    public void FogMoved_PlaysFogMove()
    {
        GameEvents.TriggerFogMoved();
        Assert.AreEqual(SfxId.FogMove, _audio.Last);
    }

    [Test]
    public void HiddenRevealed_PlaysReveal()
    {
        GameEvents.TriggerHiddenItemRevealed(null);
        Assert.AreEqual(SfxId.HiddenReveal, _audio.Last);
    }

    [Test]
    public void Coins_UpIsGain_DownIsSpend_SameIsSilent()
    {
        GameEvents.TriggerCoinsChanged(150);
        Assert.AreEqual(SfxId.CoinsGain, _audio.Last);

        GameEvents.TriggerCoinsChanged(120);
        Assert.AreEqual(SfxId.CoinsSpend, _audio.Last);

        int before = _audio.Sfx.Count;
        GameEvents.TriggerCoinsChanged(120);
        Assert.AreEqual(before, _audio.Sfx.Count);
    }

    [Test]
    public void TrayWarning_WhenTheTrayGetsDownToTwoFreeSlots()
    {
        _freeSlots = 3;
        GameEvents.TriggerTrayChanged();
        Assert.AreEqual(0, _audio.Count(SfxId.TrayWarning));

        _freeSlots = 2;
        GameEvents.TriggerTrayChanged();
        Assert.AreEqual(1, _audio.Count(SfxId.TrayWarning));

        _freeSlots = 1;
        GameEvents.TriggerTrayChanged();
        Assert.AreEqual(2, _audio.Count(SfxId.TrayWarning));
    }

    [Test]
    public void TrayWarning_NotWhenTheTrayEmpties_NorWhenFull()
    {
        _freeSlots = 2;
        GameEvents.TriggerTrayChanged();   // Warns (got fuller)
        _freeSlots = 5;
        GameEvents.TriggerTrayChanged();   // A match freed slots: no warning
        _freeSlots = 0;
        GameEvents.TriggerTrayChanged();   // Full: the lose sound handles it

        Assert.AreEqual(1, _audio.Count(SfxId.TrayWarning));
    }

    [Test]
    public void Unsubscribe_StopsListening()
    {
        _router.Unsubscribe();
        GameEvents.TriggerMatchCompleted(0);
        GameEvents.TriggerLevelCleared();

        Assert.AreEqual(0, _audio.Sfx.Count);
    }
}
