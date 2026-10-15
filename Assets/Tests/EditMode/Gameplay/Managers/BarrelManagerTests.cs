using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BarrelManagerTests
{
    private GameObject _boardObject;
    private BoardManager _boardManager;
    private BarrelManager _barrelManager;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("Board");
        _boardManager = _boardObject.AddComponent<BoardManager>();
        _boardManager.Initialize(null, new FakeItemPool(), new FakeCameraController());

        _barrelManager = _boardObject.AddComponent<BarrelManager>();
        _barrelManager.Initialize(_boardManager);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned) if (go != null) Object.DestroyImmediate(go);
        _spawned.Clear();
        Object.DestroyImmediate(_boardObject);
    }

    // ---------- Helpers ----------

    // Vertical pipe at column x, 2 cells tall, exit at (x, 0)
    private static Pipe MakePipe(int x)
    {
        return new Pipe
        {
            path = new List<Vector2Int> { new Vector2Int(x, 1), new Vector2Int(x, 0) },
            itemsQueue = new List<ItemSpawnData>(),
            activeItems = new List<Item>()
        };
    }

    // Puts an element at the exit of the pipe (index 0 of activeItems/queue)
    private Item AddItemAtExit(Pipe pipe, ItemSpawnData data)
    {
        var go = new GameObject($"Item_{data.type}");
        _spawned.Add(go);
        Item item = go.AddComponent<Item>();

        item.Init(data, Color.red, pipe.ExitPosition, pipe, null);
        if (data.type == ItemType.Barrel) item.ApplyBarrelLook(data.lives, null, Color.gray);

        pipe.activeItems.Add(item);
        pipe.itemsQueue.Add(data);
        return item;
    }

    private void SetBoardPipes(List<Pipe> pipes)
    {
        typeof(BoardManager)
            .GetField("pipes", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_boardManager, pipes);
    }

    private static ItemSpawnData Barrel(int colorId, int lives, bool contentHidden = false) =>
        new ItemSpawnData { colorId = colorId, type = ItemType.Barrel, lives = lives, contentHidden = contentHidden };

    // ---------- Neighbor rule ----------

    [Test]
    public void AreNeighbors_OnlyLeftAndRight_ByDefault()
    {
        var center = new Vector2Int(2, 0);

        Assert.IsTrue(_barrelManager.AreNeighbors(center, new Vector2Int(1, 0)));
        Assert.IsTrue(_barrelManager.AreNeighbors(center, new Vector2Int(3, 0)));
        Assert.IsFalse(_barrelManager.AreNeighbors(center, new Vector2Int(2, 1)), "Vertical neighbors are off by default.");
        Assert.IsFalse(_barrelManager.AreNeighbors(center, new Vector2Int(3, 1)), "Diagonals never count.");
        Assert.IsFalse(_barrelManager.AreNeighbors(center, new Vector2Int(4, 0)));
    }

    [Test]
    public void AreNeighbors_IncludesUpAndDown_WhenEnabled()
    {
        _barrelManager.includeVerticalNeighbors = true;

        Assert.IsTrue(_barrelManager.AreNeighbors(new Vector2Int(2, 0), new Vector2Int(2, 1)));
    }

    // ---------- Damage ----------

    [Test]
    public void CollectingFromANeighborPipe_DamagesTheBarrel()
    {
        Pipe left = MakePipe(1);
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(left, ItemSpawnData.Normal(0));
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { left, barrelPipe });

        _barrelManager.HandleItemRemovedFromBoard(left.activeItems[0], left, left.ExitPosition);

        Assert.AreEqual(2, barrel.BarrelLives);
        Assert.AreEqual(2, barrelPipe.itemsQueue[0].lives, "The queue entry must stay in sync.");
    }

    [Test]
    public void CollectingFromAFarPipe_DoesNotDamageTheBarrel()
    {
        Pipe far = MakePipe(0);
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(far, ItemSpawnData.Normal(0));
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { far, barrelPipe });

        _barrelManager.HandleItemRemovedFromBoard(far.activeItems[0], far, far.ExitPosition);

        Assert.AreEqual(3, barrel.BarrelLives);
    }

    [Test]
    public void BarrelNotAtExit_DoesNotTakeDamage()
    {
        Pipe left = MakePipe(1);
        AddItemAtExit(left, ItemSpawnData.Normal(0));

        // Barrel pipe: a normal item at the exit and the barrel behind it
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(barrelPipe, ItemSpawnData.Normal(3));
        var go = new GameObject("BarrelBehind");
        _spawned.Add(go);
        Item barrel = go.AddComponent<Item>();
        ItemSpawnData barrelData = Barrel(colorId: 1, lives: 3);
        barrel.Init(barrelData, Color.red, barrelPipe.path[0], barrelPipe, null);
        barrel.ApplyBarrelLook(3, null, Color.gray);
        barrelPipe.activeItems.Add(barrel);
        barrelPipe.itemsQueue.Add(barrelData);

        SetBoardPipes(new List<Pipe> { left, barrelPipe });

        _barrelManager.HandleItemRemovedFromBoard(left.activeItems[0], left, left.ExitPosition);

        Assert.AreEqual(3, barrel.BarrelLives, "Only a barrel sitting at the exit can be damaged.");
    }

    [Test]
    public void BarrelCantBeCollected_WhileItHasLives()
    {
        Pipe barrelPipe = MakePipe(2);
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));

        Assert.IsTrue(barrel.IsAtExit);
        Assert.IsFalse(barrel.CanBeCollected);
    }

    // ---------- Breaking ----------

    [Test]
    public void LosingAllLives_BreaksTheBarrel_IntoItsContent()
    {
        Pipe left = MakePipe(1);
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(left, ItemSpawnData.Normal(0));
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 1));
        SetBoardPipes(new List<Pipe> { left, barrelPipe });

        _barrelManager.HandleItemRemovedFromBoard(left.activeItems[0], left, left.ExitPosition);

        Assert.AreEqual(ItemType.Normal, barrel.itemType);
        Assert.AreEqual(1, barrel.colorID, "It keeps the color of the element it was holding.");
        Assert.IsTrue(barrel.CanBeCollected, "Once broken, it can be tapped from the exit.");
        Assert.AreEqual(ItemType.Normal, barrelPipe.itemsQueue[0].type);
    }

    [Test]
    public void BreakingABarrelWithHiddenContent_LeavesAHiddenItem()
    {
        Pipe barrelPipe = MakePipe(2);
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 1, contentHidden: true));
        SetBoardPipes(new List<Pipe> { barrelPipe });

        _boardManager.BreakBarrel(barrel);

        Assert.AreEqual(ItemType.Hidden, barrel.itemType);
        Assert.AreEqual(ItemType.Hidden, barrelPipe.itemsQueue[0].type);
    }

    [Test]
    public void BarrelBreaks_WhenAllNeighborPipesAreEmpty()
    {
        Pipe left = MakePipe(1);   // empty
        Pipe right = MakePipe(3);  // empty
        Pipe barrelPipe = MakePipe(2);
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { left, barrelPipe, right });

        _barrelManager.CheckIsolatedBarrels();

        Assert.IsFalse(barrel.IsBarrel, "Nothing can damage it anymore, so it must break on its own.");
    }

    [Test]
    public void BarrelDoesNotBreak_WhileANeighborPipeStillHasItems()
    {
        Pipe left = MakePipe(1);   // empty
        Pipe right = MakePipe(3);
        AddItemAtExit(right, ItemSpawnData.Normal(0));
        Pipe barrelPipe = MakePipe(2);
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { left, barrelPipe, right });

        _barrelManager.CheckIsolatedBarrels();

        Assert.IsTrue(barrel.IsBarrel);
        Assert.AreEqual(3, barrel.BarrelLives);
    }

    [Test]
    public void BarrelWithNoNeighborPipesAtAll_BreaksOnItsOwn()
    {
        Pipe barrelPipe = MakePipe(2);
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { barrelPipe });

        _barrelManager.CheckIsolatedBarrels();

        Assert.IsFalse(barrel.IsBarrel);
    }

    // ---------- Boosters ignore barrels ----------

    [Test]
    public void Boosters_IgnoreBarrels()
    {
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { barrelPipe });

        Assert.AreEqual(0, _boardManager.GetActiveColors(includeHidden: true).Count, "Match can't target a barrel's color.");
        Assert.IsNull(_boardManager.ExtractAnyItemOfColor(1), "Match can't pull a barrel out of its pipe.");
    }

    // ---------- Match booster ----------

    [Test]
    public void ItemPulledByMatchFromANeighborCell_AlsoDamagesTheBarrel()
    {
        Pipe left = MakePipe(1);
        Pipe barrelPipe = MakePipe(2);
        AddItemAtExit(left, ItemSpawnData.Normal(0));
        Item barrel = AddItemAtExit(barrelPipe, Barrel(colorId: 1, lives: 3));
        SetBoardPipes(new List<Pipe> { left, barrelPipe });

        // Subscribe to GameEvents like in Play mode (EditMode doesn't call OnEnable by itself)
        InvokePrivate(_barrelManager, "OnEnable");
        try
        {
            // This is the path the Match booster uses to take items from the board
            Item pulled = _boardManager.ExtractAnyItemOfColor(0);
            Assert.IsNotNull(pulled);
        }
        finally
        {
            InvokePrivate(_barrelManager, "OnDisable");
        }

        Assert.AreEqual(2, barrel.BarrelLives);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(target, null);
    }
}
