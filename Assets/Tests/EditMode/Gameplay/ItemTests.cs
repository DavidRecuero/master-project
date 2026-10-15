using NUnit.Framework;
using UnityEngine;

public class ItemTests
{
    private GameObject _gameObject;
    private Item _item;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("Item");
        _item = _gameObject.AddComponent<Item>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Init_SetsPropertiesAndAddsBoxCollider()
    {
        var pipe = new Pipe
        {
            layer = 1,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0) }
        };

        _item.Init(2, Color.red, new Vector2Int(0, 0), pipe, null);

        Assert.AreEqual(2, _item.colorID);
        Assert.AreEqual(Color.red, _item.itemColor);
        Assert.IsNotNull(_gameObject.GetComponent<BoxCollider2D>());
    }

    [Test]
    public void UpdateGridPosition_UpdatesPositionCorrectly()
    {
        var pipe = new Pipe
        {
            layer = 1,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) }
        };
        _item.Init(1, Color.blue, new Vector2Int(0, 0), pipe, null);

        _item.UpdateGridPosition(new Vector2Int(0, 1));

        Assert.AreEqual(new Vector2Int(0, 1), _item.gridPosition);
    }

    [Test]
    public void CanBeCollected_IsTrue_OnlyAtThePipeExit()
    {
        var pipe = new Pipe
        {
            layer = 0,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(0, 0) }
        };

        _item.Init(1, Color.blue, new Vector2Int(0, 1), pipe, null);
        Assert.IsFalse(_item.CanBeCollected, "An item in the middle of the pipe can't be collected.");

        _item.UpdateGridPosition(new Vector2Int(0, 0));
        Assert.IsTrue(_item.CanBeCollected, "An item at the exit can be collected.");
    }

    [Test]
    public void CanBeCollected_IsFalse_WhenAlreadyInTray()
    {
        var pipe = new Pipe
        {
            layer = 0,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0) }
        };
        _item.Init(1, Color.blue, new Vector2Int(0, 0), pipe, null);

        _item.inTray = true;

        Assert.IsFalse(_item.CanBeCollected);
    }

    [Test]
    public void Init_WithSpawnData_StoresTheItemType()
    {
        var pipe = new Pipe
        {
            layer = 0,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0) }
        };

        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), pipe, null);

        Assert.AreEqual(ItemType.Hidden, _item.itemType);
        Assert.AreEqual(2, _item.colorID);
    }

    [Test]
    public void ResetForPool_ClearsObstacleState()
    {
        var pipe = new Pipe
        {
            layer = 0,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0) }
        };
        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Barrel, lives = 2 }, Color.red, new Vector2Int(0, 0), pipe, null);
        _item.inTray = true;

        _item.ResetForPool();

        Assert.AreEqual(ItemType.Normal, _item.itemType);
        Assert.IsFalse(_item.inTray);
        Assert.IsNull(_item.parentPipe);
    }

    // ---------- Hidden items ----------

    private Pipe MakeSingleCellPipe()
    {
        return new Pipe
        {
            layer = 0,
            path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0, 0) }
        };
    }

    [Test]
    public void HiddenItem_ShowsHiddenColor_UntilRevealed()
    {
        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);
        _item.ApplyHiddenLook(Color.gray, null);

        var sr = _gameObject.GetComponent<SpriteRenderer>();
        Assert.AreEqual(Color.gray, sr.color, "A hidden item must not show its real color.");
        Assert.IsTrue(_item.IsHidden);
        Assert.IsFalse(_item.HasKnownColor);

        _item.Reveal();

        Assert.AreEqual(Color.red, sr.color, "Once revealed, the real color is shown.");
        Assert.AreEqual(ItemType.Normal, _item.itemType);
        Assert.AreEqual(2, _item.colorID, "Revealing must keep the original color id.");
        Assert.IsTrue(_item.HasKnownColor);
    }

    [Test]
    public void ApplyHiddenLook_DoesNothing_OnNormalItems()
    {
        _item.Init(2, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);

        _item.ApplyHiddenLook(Color.gray, null);

        Assert.AreEqual(Color.red, _gameObject.GetComponent<SpriteRenderer>().color);
    }

    [Test]
    public void ApplyColor_OnHiddenItem_StoresColor_ButKeepsItHidden()
    {
        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);
        _item.ApplyHiddenLook(Color.gray, null);

        _item.ApplyColor(1, Color.blue);

        Assert.AreEqual(1, _item.colorID);
        Assert.AreEqual(Color.gray, _gameObject.GetComponent<SpriteRenderer>().color);

        _item.Reveal();
        Assert.AreEqual(Color.blue, _gameObject.GetComponent<SpriteRenderer>().color);
    }

    [Test]
    public void HiddenItem_CanBeCollected_AtTheExit_LikeANormalItem()
    {
        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);

        Assert.IsTrue(_item.CanBeCollected);
    }

    [Test]
    public void SetIdentity_ChangesColorAndType_AndShowsItAsVisible()
    {
        _item.Init(new ItemSpawnData { colorId = 2, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);
        _item.ApplyHiddenLook(Color.gray, null);

        _item.SetIdentity(1, Color.blue, ItemType.Normal);

        Assert.AreEqual(1, _item.colorID);
        Assert.AreEqual(ItemType.Normal, _item.itemType);
        Assert.AreEqual(Color.blue, _gameObject.GetComponent<SpriteRenderer>().color);
    }

    [Test]
    public void SetIdentity_ToHidden_ThenApplyHiddenLook_CoversTheNewColor()
    {
        _item.Init(1, Color.blue, new Vector2Int(0, 0), MakeSingleCellPipe(), null);

        _item.SetIdentity(3, Color.green, ItemType.Hidden);
        _item.ApplyHiddenLook(Color.gray, null);

        Assert.IsTrue(_item.IsHidden);
        Assert.AreEqual(3, _item.colorID);
        Assert.AreEqual(Color.gray, _gameObject.GetComponent<SpriteRenderer>().color);
    }

    // ---------- Barrel ----------

    private Sprite MakeSprite()
    {
        var tex = new Texture2D(2, 2);
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);
    }

    [Test]
    public void Barrel_ShowsTheNextDamageSprite_EachTimeItLosesALife()
    {
        Sprite intact = MakeSprite(), damaged = MakeSprite(), almostBroken = MakeSprite();
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);
        _item.ApplyBarrelLook(3, new[] { intact, damaged, almostBroken }, Color.gray);

        var sr = _gameObject.GetComponent<SpriteRenderer>();
        Assert.AreSame(intact, sr.sprite);

        Assert.IsFalse(_item.TakeBarrelDamage());
        Assert.AreSame(damaged, sr.sprite);

        Assert.IsFalse(_item.TakeBarrelDamage());
        Assert.AreSame(almostBroken, sr.sprite);

        Assert.IsTrue(_item.TakeBarrelDamage(), "The third hit leaves it with 0 lives.");
    }

    [Test]
    public void BreakOpen_RestoresTheItemSpriteAndColor()
    {
        Sprite itemSprite = MakeSprite();
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), itemSprite);
        _item.ApplyBarrelLook(1, new[] { MakeSprite() }, Color.gray);

        _item.BreakOpen(itemSprite);

        var sr = _gameObject.GetComponent<SpriteRenderer>();
        Assert.AreSame(itemSprite, sr.sprite);
        Assert.AreEqual(Color.red, sr.color);
        Assert.AreEqual(ItemType.Normal, _item.itemType);
    }

    [Test]
    public void ResetForPool_ClearsBarrelLives()
    {
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);
        _item.ApplyBarrelLook(3, null, Color.gray);

        _item.ResetForPool();

        Assert.AreEqual(0, _item.BarrelLives);
        Assert.IsFalse(_item.IsBarrel);
    }

    [Test]
    public void Barrel_StartingWithFewerLivesThanSprites_ShowsTheMatchingDamagedSprite()
    {
        Sprite intact = MakeSprite(), damaged = MakeSprite(), almostBroken = MakeSprite();
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);

        _item.ApplyBarrelLook(2, new[] { intact, damaged, almostBroken }, Color.gray);

        var sr = _gameObject.GetComponent<SpriteRenderer>();
        Assert.AreSame(damaged, sr.sprite, "2 lives left -> second sprite.");

        _item.TakeBarrelDamage();
        Assert.AreSame(almostBroken, sr.sprite, "1 life left -> last sprite.");
    }

    [Test]
    public void Barrel_WithMoreLivesThanSprites_StaysIntactUntilItCatchesUp()
    {
        Sprite intact = MakeSprite(), damaged = MakeSprite(), almostBroken = MakeSprite();
        _item.Init(new ItemSpawnData { colorId = 1, type = ItemType.Barrel }, Color.red, new Vector2Int(0, 0), MakeSingleCellPipe(), null);

        _item.ApplyBarrelLook(5, new[] { intact, damaged, almostBroken }, Color.gray);
        var sr = _gameObject.GetComponent<SpriteRenderer>();

        Assert.AreSame(intact, sr.sprite, "5 lives");
        _item.TakeBarrelDamage();
        Assert.AreSame(intact, sr.sprite, "4 lives");
        _item.TakeBarrelDamage();
        Assert.AreSame(intact, sr.sprite, "3 lives");
        _item.TakeBarrelDamage();
        Assert.AreSame(damaged, sr.sprite, "2 lives");
    }
}
