using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>The crate announces every hit and its break, so visual effects can react without touching gameplay.</summary>
public class CrateEventsTests
{
    private GameObject _gameObject;
    private Item _item;
    private readonly List<int> _events = new List<int>();

    private void Record(Item barrel, int livesLeft) => _events.Add(livesLeft);

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("Crate");
        _item = _gameObject.AddComponent<Item>();
        _events.Clear();
        GameEvents.OnBarrelDamaged += Record;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.OnBarrelDamaged -= Record;
        Object.DestroyImmediate(_gameObject);
    }

    private void MakeCrate(int lives)
    {
        var pipe = new Pipe { layer = 0, path = new List<Vector2Int> { new Vector2Int(0, 0) } };
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), pipe);
        _item.ApplyBarrelLook(lives, null);
    }

    [Test]
    public void EveryHit_ThatLeavesLives_IsAnnounced()
    {
        MakeCrate(3);

        _item.TakeBarrelDamage();
        _item.TakeBarrelDamage();

        CollectionAssert.AreEqual(new[] { 2, 1 }, _events);
    }

    [Test]
    public void TheBreak_IsAnnouncedOnce_ByBreakOpen()
    {
        MakeCrate(1);

        bool destroyed = _item.TakeBarrelDamage();
        Assert.IsTrue(destroyed);
        Assert.AreEqual(0, _events.Count, "The last hit waits for the crate to actually burst.");

        _item.BreakOpen();

        CollectionAssert.AreEqual(new[] { 0 }, _events);
    }

    [Test]
    public void NormalItems_NeverAnnounceDamage()
    {
        var pipe = new Pipe { layer = 0, path = new List<Vector2Int> { new Vector2Int(0, 0) } };
        _item.Init(2, Color.blue, new Vector2Int(0, 0), pipe);

        _item.TakeBarrelDamage();
        _item.BreakOpen();

        Assert.AreEqual(0, _events.Count);
    }

    [Test]
    public void PlankThatFliesOff_MatchesTheSprites()
    {
        Assert.AreEqual(1, CrateDebrisEffect.PlankRemovedAt(2), "3 -> 2 lives: the middle plank goes.");
        Assert.AreEqual(0, CrateDebrisEffect.PlankRemovedAt(1), "2 -> 1 lives: the top plank goes.");
        Assert.AreEqual(-1, CrateDebrisEffect.PlankRemovedAt(4), "Above 3 lives the sprite doesn't change.");
    }
}
