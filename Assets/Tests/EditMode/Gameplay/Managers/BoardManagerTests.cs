using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManagerTests
{
    private GameObject _boardObject;
    private BoardManager _boardManager;
    private FakeItemPool _fakePool;
    private FakeCameraController _fakeCam;

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("BoardManager");
        _boardManager = _boardObject.AddComponent<BoardManager>();

        var tilemapGo = new GameObject("Tilemap");
        tilemapGo.transform.SetParent(_boardObject.transform);
        _boardManager.tilemap = tilemapGo.AddComponent<Tilemap>();
        _boardManager.tiles = new TileBase[] { ScriptableObject.CreateInstance<Tile>() };

        _fakePool = new FakeItemPool();
        _fakeCam = new FakeCameraController();

        _boardManager.Initialize(null, _fakePool, _fakeCam);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_boardObject);
    }

    [Test]
    public void IsBoardCleared_ReturnsTrue_WhenNoPipesExist()
    {
        Assert.IsTrue(_boardManager.IsBoardCleared());
    }

    [Test]
    public void IsBoardCleared_ReturnsFalse_WhenActiveItemsRemainInPipes()
    {
        var itemGo = new GameObject("ActiveItem");
        var item = itemGo.AddComponent<Item>();

        var pipesField = typeof(BoardManager).GetField("pipes", BindingFlags.NonPublic | BindingFlags.Instance);
        var pipes = new List<Pipe>
        {
            new Pipe { activeItems = new List<Item> { item } }
        };
        pipesField.SetValue(_boardManager, pipes);

        Assert.IsFalse(_boardManager.IsBoardCleared());
    }
}