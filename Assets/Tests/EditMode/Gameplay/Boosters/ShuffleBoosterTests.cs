using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ShuffleBoosterTests
{
    private GameObject _boardObject;
    private BoardManager _boardManager;

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("BoardManager");
        _boardManager = _boardObject.AddComponent<BoardManager>();
        _boardManager.Initialize(null, new FakeItemPool(), new FakeCameraController());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_boardObject);
    }

    private void SetBoardPipes(List<Pipe> pipes)
    {
        typeof(BoardManager)
            .GetField("pipes", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_boardManager, pipes);
    }

    private static Item MakeItem(int colorId)
    {
        var go = new GameObject($"Item_{colorId}");
        Item item = go.AddComponent<Item>();
        item.colorID = colorId;
        return item;
    }

    [Test]
    public void CanExecute_ReturnsFalse_WhenBoardManagerIsNull()
    {
        var booster = new ShuffleBooster(null);

        Assert.IsFalse(booster.CanExecute());
    }

    [Test]
    public void CanExecute_ReturnsTrue_WhenBoardManagerIsAssigned()
    {
        var booster = new ShuffleBooster(_boardManager);

        Assert.IsTrue(booster.CanExecute());
    }

    [Test]
    public void Execute_PreservesTheTotalCountOfEachColor()
    {
        // Distribution before: color 0 x2, color 1 x1, color 2 x1, color 3 x1
        var pipe1 = new Pipe
        {
            path = new List<Vector2Int>(),
            itemsQueue = TestQueues.Of(0, 0, 1),
            activeItems = new List<Item> { MakeItem(0), MakeItem(0), MakeItem(1) }
        };
        var pipe2 = new Pipe
        {
            path = new List<Vector2Int>(),
            itemsQueue = TestQueues.Of(2, 3),
            activeItems = new List<Item> { MakeItem(2), MakeItem(3) }
        };
        SetBoardPipes(new List<Pipe> { pipe1, pipe2 });

        var booster = new ShuffleBooster(_boardManager);
        booster.Execute();

        List<int> colorsAfter = pipe1.activeItems.Concat(pipe2.activeItems).Select(i => i.colorID).OrderBy(c => c).ToList();
        CollectionAssert.AreEqual(new List<int> { 0, 0, 1, 2, 3 }, colorsAfter,
            "Shuffling must redistribute the same multiset of colors, never add/remove/change the total counts.");

        // itemsQueue must stay in sync with the (possibly reshuffled) activeItems colors
        CollectionAssert.AreEqual(pipe1.activeItems.Select(i => i.colorID).ToList(), TestQueues.Colors(pipe1.itemsQueue));
        CollectionAssert.AreEqual(pipe2.activeItems.Select(i => i.colorID).ToList(), TestQueues.Colors(pipe2.itemsQueue));
    }

    [Test]
    public void Execute_DoesNothing_WhenFewerThanTwoItemsAreActive()
    {
        var onlyItem = MakeItem(1);
        var pipe = new Pipe
        {
            path = new List<Vector2Int>(),
            itemsQueue = TestQueues.Of(1),
            activeItems = new List<Item> { onlyItem }
        };
        SetBoardPipes(new List<Pipe> { pipe });

        var booster = new ShuffleBooster(_boardManager);

        Assert.DoesNotThrow(() => booster.Execute());
        Assert.AreEqual(1, onlyItem.colorID, "With only 1 item in play there's nothing to shuffle against.");
    }

    [Test]
    public void Execute_LeavesHiddenItemsUntouched()
    {
        var hidden = MakeItem(3);
        hidden.itemType = ItemType.Hidden;

        var pipe = new Pipe
        {
            path = new List<Vector2Int>(),
            itemsQueue = TestQueues.Of(3, 0, 1, 2),
            activeItems = new List<Item> { hidden, MakeItem(0), MakeItem(1), MakeItem(2) }
        };
        pipe.itemsQueue[0].type = ItemType.Hidden;
        SetBoardPipes(new List<Pipe> { pipe });

        new ShuffleBooster(_boardManager).Execute();

        Assert.AreEqual(3, hidden.colorID, "Hidden items must keep their color.");
        Assert.AreEqual(ItemType.Hidden, hidden.itemType);
        Assert.AreEqual(3, pipe.itemsQueue[0].colorId);
    }
}
