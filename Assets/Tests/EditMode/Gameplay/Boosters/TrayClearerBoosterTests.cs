using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TrayClearerBoosterTests
{
    private GameObject _trayObject;
    private GameObject _boardObject;
    private TrayManager _trayManager;
    private BoardManager _boardManager;
    private FakeItemPool _fakePool;
    private TrayClearerBooster _booster;

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("BoardManager");
        _boardManager = _boardObject.AddComponent<BoardManager>();
        _boardManager.Initialize(null, new FakeItemPool(), new FakeCameraController());

        _fakePool = new FakeItemPool();
        _trayObject = new GameObject("TrayManager");
        _trayManager = _trayObject.AddComponent<TrayManager>();
        _trayManager.Initialize(_fakePool, new FakeGameStateController(), new FakeCameraController());

        _booster = new TrayClearerBooster(_trayManager, _boardManager);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_trayObject);
        Object.DestroyImmediate(_boardObject);
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

    private static Item MakeItem(int colorId)
    {
        var go = new GameObject($"Item_{colorId}");
        Item item = go.AddComponent<Item>();
        item.colorID = colorId;
        return item;
    }

    // A pipe that's already "full" (activeItems.Count == path.Count) so AddItemToRandomPipe
    // only appends to itemsQueue instead of spawning a new GameObject - keeps the test free
    // of any Tilemap dependency.
    private static Pipe CreateFullPipe(int existingColorId)
    {
        return new Pipe
        {
            path = new List<Vector2Int> { new Vector2Int(0, 0) },
            itemsQueue = new List<int> { existingColorId },
            activeItems = new List<Item> { MakeItem(existingColorId) }
        };
    }

    [Test]
    public void CanExecute_ReturnsFalse_WhenTrayIsEmpty()
    {
        Assert.IsFalse(_booster.CanExecute());
    }

    [Test]
    public void CanExecute_ReturnsTrue_WhenTrayHasItems()
    {
        GetTrayItemsField().Add(MakeItem(1));

        Assert.IsTrue(_booster.CanExecute());
    }

    [Test]
    public void Execute_EmptiesTheTray_AndQueuesEachItemBackIntoTheBoard()
    {
        // Only 1 pipe exists, so which one gets picked is deterministic.
        Pipe onlyPipe = CreateFullPipe(existingColorId: 9);
        SetBoardPipes(new List<Pipe> { onlyPipe });

        List<Item> trayItems = GetTrayItemsField();
        trayItems.Add(MakeItem(5));
        trayItems.Add(MakeItem(6));

        _booster.Execute();

        Assert.AreEqual(0, GetTrayItemsField().Count, "Tray should be completely emptied.");
        CollectionAssert.AreEqual(new List<int> { 9, 5, 6 }, onlyPipe.itemsQueue,
            "Both cleared items should have been appended to the only pipe's queue, in tray order.");
        Assert.AreEqual(1, onlyPipe.activeItems.Count, "The pipe was already full, so no new item should spawn immediately.");
        Assert.AreEqual(2, _fakePool.ReleasedItemsCount, "Each tray item should be released back to the pool.");
    }

    [Test]
    public void Execute_DoesNothing_WhenTrayIsAlreadyEmpty()
    {
        SetBoardPipes(new List<Pipe> { CreateFullPipe(existingColorId: 1) });

        Assert.DoesNotThrow(() => _booster.Execute());
        Assert.AreEqual(0, _fakePool.ReleasedItemsCount);
    }
}
