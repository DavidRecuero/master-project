using NUnit.Framework;
using UnityEngine;

/// <summary>Items under a higher pipe: hidden at rest, never turned off (so their movement goes on).</summary>
public class ItemCoverTests
{
    private GameObject _go;
    private Item _item;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Item");
        _go.AddComponent<BoxCollider2D>();
        _item = _go.AddComponent<Item>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    private bool Drawn => _go.GetComponent<SpriteRenderer>().enabled;
    private bool Tappable => _go.GetComponent<Collider2D>().enabled;

    [Test]
    public void Covered_AtRest_IsHiddenAndNotTappable_ButStaysActive()
    {
        _item.SetCovered(true);

        Assert.IsTrue(_go.activeSelf, "Turning it off would stop its movement");
        Assert.IsFalse(Drawn);
        Assert.IsFalse(Tappable);
    }

    [Test]
    public void Uncovered_IsDrawnAndTappable()
    {
        _item.SetCovered(true);
        _item.SetCovered(false);

        Assert.IsTrue(Drawn);
        Assert.IsTrue(Tappable);
    }

    [Test]
    public void InTheTray_IsAlwaysDrawn()
    {
        _item.inTray = true;
        _item.SetCovered(true);

        Assert.IsTrue(Drawn);
    }

    [Test]
    public void ResetForPool_ClearsTheCover()
    {
        _item.SetCovered(true);

        _item.ResetForPool();

        Assert.IsFalse(_item.IsCovered);
        Assert.IsTrue(Drawn);
        Assert.IsTrue(Tappable);
    }
}
