using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MatchBoosterTests
{
    private GameObject _trayObject;
    private GameObject _boardObject;
    private TrayManager _trayManager;
    private BoardManager _boardManager;
    private MatchBooster _booster;

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("BoardManager");
        _boardManager = _boardObject.AddComponent<BoardManager>();
        _boardManager.Initialize(null, new FakeItemPool(), new FakeCameraController());

        _trayObject = new GameObject("TrayManager");
        _trayManager = _trayObject.AddComponent<TrayManager>();
        _trayManager.Initialize(new FakeItemPool(), new FakeGameStateController(), new FakeCameraController());

        // Wire the tray to the board directly via reflection, same as production Awake() would.
        typeof(TrayManager)
            .GetField("boardManager", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_trayManager, _boardManager);

        _booster = new MatchBooster(_trayManager, _boardManager);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_trayObject);
        Object.DestroyImmediate(_boardObject);
    }

    // Builds a pipe with exactly one active item of the given color. A single-item pipe
    // keeps ExtractAnyItemOfColor's reposition/respawn branches from ever running, so the
    // test doesn't need a real Tilemap.
    private static Pipe CreateSingleItemPipe(int colorId)
    {
        var itemGo = new GameObject($"Item_{colorId}");
        Item item = itemGo.AddComponent<Item>();
        item.colorID = colorId;

        return new Pipe
        {
            path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
            itemsQueue = TestQueues.Of(colorId),
            activeItems = new List<Item> { item }
        };
    }

    private void SetBoardPipes(List<Pipe> pipes)
    {
        typeof(BoardManager)
            .GetField("pipes", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_boardManager, pipes);
    }

    private List<Item> GetTrayItemsField()
    {
        var field = typeof(TrayManager).GetField("trayItems", BindingFlags.NonPublic | BindingFlags.Instance);
        return (List<Item>)field.GetValue(_trayManager);
    }

    [Test]
    public void CanExecute_ReturnsFalse_WhenBoardAndTrayAreBothEmpty()
    {
        SetBoardPipes(new List<Pipe>());

        Assert.IsFalse(_booster.CanExecute());
    }

    [Test]
    public void CanExecute_ReturnsTrue_WhenBoardStillHasItems()
    {
        SetBoardPipes(new List<Pipe> { CreateSingleItemPipe(colorId: 1) });

        Assert.IsTrue(_booster.CanExecute());
    }

    [Test]
    public void Execute_PrioritizesTrayColor_OverBoard()
    {
        // Tray already has 1 red (colorID 0); needs 2 more to match (matchSize = 3).
        var trayRed = new GameObject("TrayRed").AddComponent<Item>();
        trayRed.colorID = 0;
        GetTrayItemsField().Add(trayRed);

        // Board only has 1 red available (not enough to complete the match by itself),
        // plus a decoy blue that should be left untouched.
        var pipes = new List<Pipe>
        {
            CreateSingleItemPipe(colorId: 0), // red
            CreateSingleItemPipe(colorId: 1)  // blue - should NOT be touched
        };
        SetBoardPipes(pipes);

        _booster.Execute();

        List<Item> trayItems = GetTrayItemsField();
        Assert.AreEqual(2, trayItems.Count(i => i.colorID == 0), "Should have pulled the 1 available red into the tray.");
        Assert.AreEqual(0, pipes[0].activeItems.Count, "The red pipe should have been emptied.");
        Assert.AreEqual(1, pipes[1].activeItems.Count, "The blue pipe should be untouched.");
    }

    [Test]
    public void Execute_PicksBoardColor_WhenTrayIsEmpty()
    {
        // Only one color (2) exists anywhere on the board, so the "random" pick is deterministic.
        var pipes = new List<Pipe>
        {
            CreateSingleItemPipe(colorId: 2),
            CreateSingleItemPipe(colorId: 2)
        };
        SetBoardPipes(pipes);

        _booster.Execute();

        List<Item> trayItems = GetTrayItemsField();
        Assert.AreEqual(2, trayItems.Count, "Both available items of the only board color should have been collected.");
        Assert.IsTrue(trayItems.All(i => i.colorID == 2));
        Assert.IsTrue(pipes.All(p => p.activeItems.Count == 0));
    }

    [Test]
    public void Execute_DoesNothing_WhenCanExecuteIsFalse()
    {
        SetBoardPipes(new List<Pipe>());

        _booster.Execute();

        Assert.AreEqual(0, GetTrayItemsField().Count);
    }

    // ---------- Hidden items ----------

    private static Pipe CreateSingleHiddenItemPipe(int colorId)
    {
        Pipe pipe = CreateSingleItemPipe(colorId);
        pipe.activeItems[0].itemType = ItemType.Hidden;
        pipe.itemsQueue[0].type = ItemType.Hidden;
        return pipe;
    }

    [Test]
    public void CanExecute_ReturnsTrue_WhenOnlyHiddenItemsRemain()
    {
        SetBoardPipes(new List<Pipe> { CreateSingleHiddenItemPipe(colorId: 1) });

        Assert.IsTrue(_booster.CanExecute(), "Match can fall back to hidden items.");
    }

    [Test]
    public void Execute_PrefersVisibleItems_OverHiddenOnes()
    {
        var trayRed = new GameObject("TrayRed").AddComponent<Item>();
        trayRed.colorID = 0;
        GetTrayItemsField().Add(trayRed);

        // Needs 2 more reds. The hidden one comes first in pipe order, but 2 visible reds exist.
        var pipes = new List<Pipe>
        {
            CreateSingleHiddenItemPipe(colorId: 0),
            CreateSingleItemPipe(colorId: 0),
            CreateSingleItemPipe(colorId: 0)
        };
        SetBoardPipes(pipes);

        _booster.Execute();

        Assert.AreEqual(1, pipes[0].activeItems.Count, "The hidden red must stay: there were enough visible reds.");
        Assert.AreEqual(0, pipes[1].activeItems.Count);
        Assert.AreEqual(0, pipes[2].activeItems.Count);
    }

    [Test]
    public void Execute_FallsBackToHiddenItems_WhenNotEnoughVisibleOnes()
    {
        var trayRed = new GameObject("TrayRed").AddComponent<Item>();
        trayRed.colorID = 0;
        GetTrayItemsField().Add(trayRed);

        var pipes = new List<Pipe>
        {
            CreateSingleHiddenItemPipe(colorId: 0),
            CreateSingleItemPipe(colorId: 0)
        };
        SetBoardPipes(pipes);
        Item hiddenRed = pipes[0].activeItems[0];

        _booster.Execute();

        Assert.IsTrue(pipes.All(p => p.activeItems.Count == 0), "Both reds should have been pulled.");
        Assert.IsTrue(hiddenRed.inTray, "The hidden red should have been sent to the tray.");
        Assert.AreEqual(ItemType.Normal, hiddenRed.itemType, "A hidden item pulled into the tray gets revealed.");
    }

    [Test]
    public void Execute_PicksAHiddenColor_WhenTrayIsEmpty_AndNothingIsVisible()
    {
        var pipes = new List<Pipe>
        {
            CreateSingleHiddenItemPipe(colorId: 2),
            CreateSingleHiddenItemPipe(colorId: 2)
        };
        SetBoardPipes(pipes);

        _booster.Execute();

        Assert.AreEqual(2, GetTrayItemsField().Count);
        Assert.IsTrue(GetTrayItemsField().All(i => i.colorID == 2));
    }

    // ---------- Barrels ----------

    private static Pipe CreateSingleBarrelPipe(int colorId)
    {
        Pipe pipe = CreateSingleItemPipe(colorId);
        pipe.activeItems[0].itemType = ItemType.Barrel;
        pipe.itemsQueue[0].type = ItemType.Barrel;
        return pipe;
    }

    [Test]
    public void CanExecute_ReturnsTrue_WhenOnlyBarrelsRemain()
    {
        SetBoardPipes(new List<Pipe> { CreateSingleBarrelPipe(colorId: 2) });

        Assert.IsTrue(_booster.CanExecute());
    }

    [Test]
    public void Execute_FallsBackToBarrelContent_WhenNothingElseHasThatColor()
    {
        var trayRed = new GameObject("TrayRed").AddComponent<Item>();
        trayRed.colorID = 0;
        GetTrayItemsField().Add(trayRed);

        var pipes = new List<Pipe> { CreateSingleBarrelPipe(colorId: 0) };
        SetBoardPipes(pipes);
        Item barrel = pipes[0].activeItems[0];

        _booster.Execute();

        Assert.AreEqual(0, pipes[0].activeItems.Count, "The barrel's content should have been pulled.");
        Assert.IsTrue(barrel.inTray);
        Assert.IsFalse(barrel.IsBarrel, "It arrives at the tray as a normal item, not as a barrel.");
    }
}
