using NUnit.Framework;

public class GameplayHapticsRouterTests
{
    private FakeHapticsService _haptics;
    private GameplayHapticsRouter _router;
    private int _freeSlots;

    [SetUp]
    public void SetUp()
    {
        _haptics = new FakeHapticsService();
        _freeSlots = 7;
        _router = new GameplayHapticsRouter(_haptics, () => _freeSlots);
        _router.Subscribe();
    }

    [TearDown]
    public void TearDown()
    {
        _router.Unsubscribe();
    }

    [Test]
    public void PlayerTap_GivesTheSmallestTick()
    {
        GameEvents.TriggerItemCollectedByPlayer(null, null);
        Assert.AreEqual(HapticType.Selection, _haptics.Last);
    }

    [Test]
    public void ItemLanding_DoesNotVibrate()
    {
        GameEvents.TriggerItemLandedInTray(null);
        Assert.AreEqual(0, _haptics.Played.Count);
    }

    [Test]
    public void FogAndCoins_DoNotVibrate()
    {
        GameEvents.TriggerFogMoved();
        GameEvents.TriggerCoinsChanged(500);
        GameEvents.TriggerHiddenItemRevealed(null);

        Assert.AreEqual(0, _haptics.Played.Count);
    }

    [Test]
    public void Match_GivesAMediumImpact()
    {
        GameEvents.TriggerMatchCompleted(0);
        Assert.AreEqual(HapticType.MediumImpact, _haptics.Last);
    }

    [Test]
    public void LevelCleared_GivesSuccess()
    {
        GameEvents.TriggerLevelCleared();
        Assert.AreEqual(HapticType.Success, _haptics.Last);
    }

    [Test]
    public void LevelFailed_GivesFailure()
    {
        GameEvents.TriggerLevelFailed();
        Assert.AreEqual(HapticType.Failure, _haptics.Last);
    }

    [Test]
    public void LevelEnd_OnlyOnce()
    {
        GameEvents.TriggerLevelCleared();
        GameEvents.TriggerLevelFailed();

        Assert.AreEqual(1, _haptics.Played.Count);
    }

    [Test]
    public void AfterTheLevelEnds_TapsDoNotVibrate()
    {
        GameEvents.TriggerLevelCleared();
        GameEvents.TriggerItemCollectedByPlayer(null, null);

        Assert.AreEqual(0, _haptics.Count(HapticType.Selection));
    }

    [Test]
    public void LastFreeSlot_GivesAWarning()
    {
        _freeSlots = 1;
        GameEvents.TriggerTrayChanged();

        Assert.AreEqual(HapticType.Warning, _haptics.Last);
    }

    [Test]
    public void TwoFreeSlots_NoWarningYet()
    {
        _freeSlots = 2;
        GameEvents.TriggerTrayChanged();

        Assert.AreEqual(0, _haptics.Count(HapticType.Warning));
    }

    [Test]
    public void TrayWarning_OnlyOnceWhileInTheDangerZone()
    {
        _freeSlots = 1;
        GameEvents.TriggerTrayChanged();
        GameEvents.TriggerTrayChanged(); // The tray stays at 1 free

        Assert.AreEqual(1, _haptics.Count(HapticType.Warning));
    }

    [Test]
    public void TrayWarning_AgainAfterLeavingTheDangerZone()
    {
        _freeSlots = 1;
        GameEvents.TriggerTrayChanged();
        _freeSlots = 4; // A match emptied it
        GameEvents.TriggerTrayChanged();
        _freeSlots = 1;
        GameEvents.TriggerTrayChanged();

        Assert.AreEqual(2, _haptics.Count(HapticType.Warning));
    }

    [Test]
    public void TrayFull_IsLeftToTheLostLevel()
    {
        _freeSlots = 0;
        GameEvents.TriggerTrayChanged();

        Assert.AreEqual(0, _haptics.Count(HapticType.Warning));
    }

    [Test]
    public void Boosters_TrayClearerHitsHarder()
    {
        GameEvents.TriggerBoosterUsed(BoosterType.Match);
        GameEvents.TriggerBoosterUsed(BoosterType.TrayClearer);

        Assert.AreEqual(HapticType.MediumImpact, _haptics.Played[0]);
        Assert.AreEqual(HapticType.HeavyImpact, _haptics.Played[1]);
    }

    [Test]
    public void BoosterDenied_GivesAWarning()
    {
        GameEvents.TriggerBoosterDenied(BoosterType.Shuffle);
        Assert.AreEqual(HapticType.Warning, _haptics.Last);
    }

    [Test]
    public void Barrel_HitIsLight_BreakIsHeavy()
    {
        GameEvents.TriggerBarrelDamaged(null, 2);
        GameEvents.TriggerBarrelDamaged(null, 0);

        Assert.AreEqual(HapticType.LightImpact, _haptics.Played[0]);
        Assert.AreEqual(HapticType.HeavyImpact, _haptics.Played[1]);
    }

    [Test]
    public void Unsubscribe_StopsListening()
    {
        _router.Unsubscribe();
        GameEvents.TriggerMatchCompleted(0);

        Assert.AreEqual(0, _haptics.Played.Count);
    }
}
