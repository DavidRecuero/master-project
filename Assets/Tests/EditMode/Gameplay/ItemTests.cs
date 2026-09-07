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
}