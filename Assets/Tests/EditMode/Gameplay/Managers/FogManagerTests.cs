using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FogManagerTests
{
    private GameObject _boardObject;
    private BoardManager _boardManager;
    private FogManager _fogManager;

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("Board");
        _boardManager = _boardObject.AddComponent<BoardManager>();
        _fogManager = _boardObject.AddComponent<FogManager>();
        _fogManager.Initialize(_boardManager);

        // 3x3 board with one vertical pipe in column 1 whose exit is (1, 0)
        SetPrivate(_boardManager, "width", 3);
        SetPrivate(_boardManager, "height", 3);
        SetPrivate(_boardManager, "pipes", new List<Pipe>
        {
            new Pipe { path = new List<Vector2Int> { new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(1, 0) } }
        });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_boardObject);
    }

    private static void SetPrivate(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    [Test]
    public void IsAllowedCell_RejectsPipeExits()
    {
        Assert.IsFalse(_fogManager.IsAllowedCell(new Vector2Int(1, 0)), "Fog must never hide an item that is ready to be tapped.");
    }

    [Test]
    public void IsAllowedCell_RejectsCellsOutsideTheBoard()
    {
        Assert.IsFalse(_fogManager.IsAllowedCell(new Vector2Int(-1, 1)));
        Assert.IsFalse(_fogManager.IsAllowedCell(new Vector2Int(3, 1)));
    }

    [Test]
    public void IsAllowedCell_AcceptsPipeCellsThatAreNotExits()
    {
        Assert.IsTrue(_fogManager.IsAllowedCell(new Vector2Int(1, 1)));
    }

    [Test]
    public void IsAllowedCell_OnlyOverPipes_CanBeTurnedOff()
    {
        var emptyCell = new Vector2Int(0, 2); // No pipe there

        _fogManager.onlyOverPipes = true;
        Assert.IsFalse(_fogManager.IsAllowedCell(emptyCell));

        _fogManager.onlyOverPipes = false;
        Assert.IsTrue(_fogManager.IsAllowedCell(emptyCell));
    }
}
