using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public class InputManagerTests
{
    private GameObject _inputGo;
    private InputManager _input;
    private GameObject _itemGo;
    private Item _item;
    private readonly List<Item> _clicked = new List<Item>();

    [SetUp]
    public void SetUp()
    {
        _inputGo = new GameObject("InputManager");
        _input = _inputGo.AddComponent<InputManager>();
        _itemGo = new GameObject("Item");
        _item = _itemGo.AddComponent<Item>();

        // EditMode: Unity doesn't call OnEnable on AddComponent, so it is called by hand
        Invoke(_input, "OnEnable");

        _clicked.Clear();
        InputManager.OnItemClicked += Record;
    }

    [TearDown]
    public void TearDown()
    {
        InputManager.OnItemClicked -= Record;
        Invoke(_input, "OnDisable");
        Object.DestroyImmediate(_inputGo);
        Object.DestroyImmediate(_itemGo);
    }

    private void Record(Item item) => _clicked.Add(item);

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(target, null);
    }

    [Test]
    public void PressingAnItem_ReachesTheGameplay()
    {
        _item.OnPointerDown(null);

        CollectionAssert.AreEqual(new[] { _item }, _clicked);
    }

    [Test]
    public void AfterTheLevelIsCleared_PressesAreIgnored()
    {
        GameEvents.TriggerLevelCleared();

        _item.OnPointerDown(null);

        Assert.IsEmpty(_clicked);
    }

    [Test]
    public void AfterTheLevelIsFailed_PressesAreIgnored()
    {
        GameEvents.TriggerLevelFailed();

        _item.OnPointerDown(null);

        Assert.IsEmpty(_clicked);
    }

    [Test]
    public void Disabled_StopsListening()
    {
        Invoke(_input, "OnDisable");

        _item.OnPointerDown(null);

        Assert.IsEmpty(_clicked);
        Invoke(_input, "OnEnable");   // TearDown disables it again
    }

    [Test]
    public void EnsurePhysicsRaycaster_KeepsTheOneInTheScene()
    {
        var cameraGo = new GameObject("Camera", typeof(Camera));
        try
        {
            var existing = cameraGo.AddComponent<Physics2DRaycaster>();

            Assert.AreSame(existing, InputManager.EnsurePhysicsRaycaster(cameraGo.GetComponent<Camera>()));
            Assert.AreEqual(1, cameraGo.GetComponents<Physics2DRaycaster>().Length);
        }
        finally
        {
            Object.DestroyImmediate(cameraGo);
        }
    }

    [Test]
    public void EnsurePhysicsRaycaster_AddsItIfMissing()
    {
        var cameraGo = new GameObject("Camera", typeof(Camera));
        try
        {
            LogAssert.Expect(LogType.Warning, "[InputManager] The camera had no Physics2DRaycaster. Added at runtime: add it to the scene.");

            Assert.IsNotNull(InputManager.EnsurePhysicsRaycaster(cameraGo.GetComponent<Camera>()));
        }
        finally
        {
            Object.DestroyImmediate(cameraGo);
        }
    }
}
