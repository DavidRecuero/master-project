using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TrayManagerTests
{
    private GameObject _trayObject;
    private TrayManager _trayManager;
    private FakeItemPool _fakePool;
    private FakeGameStateController _fakeStateController;
    private FakeCameraController _fakeCamController;

    [SetUp]
    public void SetUp()
    {
        _trayObject = new GameObject("TrayManager");
        _trayManager = _trayObject.AddComponent<TrayManager>();

        _fakePool = new FakeItemPool();
        _fakeStateController = new FakeGameStateController();
        _fakeCamController = new FakeCameraController();

        _trayManager.Initialize(_fakePool, _fakeStateController, _fakeCamController);

        typeof(TrayManager)
            .GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_trayManager, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (_trayManager != null)
        {
            typeof(TrayManager)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(_trayManager, null);
        }
        Object.DestroyImmediate(_trayObject);
    }

    [Test]
    public void TryAddItem_ReturnsFalse_WhenTrayIsFull()
    {
        _trayManager.maxCapacity = 2;

        var item1 = new GameObject("Item1").AddComponent<Item>();
        var item2 = new GameObject("Item2").AddComponent<Item>();
        var item3 = new GameObject("Item3").AddComponent<Item>();

        Assert.IsTrue(_trayManager.TryAddItem(item1));
        Assert.IsTrue(_trayManager.TryAddItem(item2));
        Assert.IsFalse(_trayManager.TryAddItem(item3));
    }

    [Test]
    public void TryAddItem_GroupsSameColorItems_Contiguously()
    {
        _trayManager.maxCapacity = 7;

        var itemRed1 = new GameObject("Red1").AddComponent<Item>();
        itemRed1.colorID = 1;

        var itemBlue = new GameObject("Blue").AddComponent<Item>();
        itemBlue.colorID = 2;

        var itemRed2 = new GameObject("Red2").AddComponent<Item>();
        itemRed2.colorID = 1;

        _trayManager.TryAddItem(itemRed1);
        _trayManager.TryAddItem(itemBlue);
        _trayManager.TryAddItem(itemRed2);

        var trayItemsField = typeof(TrayManager).GetField("trayItems", BindingFlags.NonPublic | BindingFlags.Instance);
        var trayItems = (List<Item>)trayItemsField.GetValue(_trayManager);

        Assert.AreEqual(1, trayItems[0].colorID);
        Assert.AreEqual(1, trayItems[1].colorID);
        Assert.AreEqual(2, trayItems[2].colorID);
    }
}