using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PopupServiceTests
{
    private GameObject _serviceGo;
    private PopupService _service;
    private CountingPopup _prefabA;
    private CountingPopup _prefabB;
    private CountingPopup _prefabC;

    [SetUp]
    public void SetUp()
    {
        // Awake doesn't run in EditMode: the service builds its canvas on first use
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();

        _prefabA = PopupTestUtils.MakePrefab<CountingPopup>("PopupA");
        _prefabB = PopupTestUtils.MakePrefab<CountingPopup>("PopupB");
        _prefabC = PopupTestUtils.MakePrefab<CountingPopup>("PopupC");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefabA.gameObject);
        Object.DestroyImmediate(_prefabB.gameObject);
        Object.DestroyImmediate(_prefabC.gameObject);
    }

    [Test]
    public void Open_CreatesACopyOnThePopupCanvas_AndPutsItOnTop()
    {
        CountingPopup popup = _service.Open(_prefabA);

        Assert.IsNotNull(popup);
        Assert.AreNotSame(_prefabA, popup, "The prefab itself is never shown, only a copy");
        Assert.AreSame(_service.Root, popup.transform.parent);
        Assert.IsNotNull(_service.Root.GetComponent<Canvas>());
        Assert.AreSame(popup, _service.Top);
        Assert.AreEqual(1, _service.Count);
        Assert.AreEqual(1, popup.OpenedCalls);
        Assert.AreSame(_service, popup.Service);
        Assert.AreSame(_prefabA, popup.SourcePrefab);
    }

    [Test]
    public void Open_FillsTheWholeScreen()
    {
        var rect = (RectTransform)_service.Open(_prefabA).transform;

        Assert.AreEqual(Vector2.zero, rect.anchorMin);
        Assert.AreEqual(Vector2.one, rect.anchorMax);
        Assert.AreEqual(Vector2.zero, rect.offsetMin);
        Assert.AreEqual(Vector2.zero, rect.offsetMax);
    }

    [Test]
    public void Open_SamePrefabTwice_KeepsTheOpenCopy()
    {
        CountingPopup first = _service.Open(_prefabA);
        CountingPopup second = _service.Open(_prefabA);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, _service.Count);
        Assert.AreEqual(1, first.OpenedCalls);
    }

    [Test]
    public void Open_NullPrefab_WarnsAndOpensNothing()
    {
        LogAssert.Expect(LogType.Warning, "[PopupService] Tried to open a popup with no prefab assigned.");

        Assert.IsNull(_service.Open<CountingPopup>(null));
        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void CloseTop_GoesBackToThePopupBelow()
    {
        CountingPopup settings = _service.Open(_prefabA);
        CountingPopup credits = _service.Open(_prefabB);
        Assert.AreSame(credits, _service.Top);

        Assert.IsTrue(_service.CloseTop());

        Assert.AreSame(settings, _service.Top);
        Assert.AreEqual(1, _service.Count);
        Assert.IsTrue(credits == null, "The closed popup is destroyed");
    }

    [Test]
    public void CloseTop_WithNothingOpen_ReturnsFalse()
    {
        Assert.IsFalse(_service.CloseTop());
        Assert.IsNull(_service.Top);
    }

    [Test]
    public void Close_APopupBelow_AlsoClosesThePopupsOpenedOnTopOfIt()
    {
        CountingPopup a = _service.Open(_prefabA);
        _service.Open(_prefabB);
        _service.Open(_prefabC);

        _service.Close(a);

        Assert.AreEqual(0, _service.Count);
        Assert.IsNull(_service.Top);
    }

    [Test]
    public void Close_NotifiesThePopupBeforeDestroyingIt()
    {
        CountingPopup a = _service.Open(_prefabA);
        CountingPopup b = _service.Open(_prefabB);

        _service.Close(b);

        // The C# object is still readable after Unity destroys it
        Assert.AreEqual(1, b.ClosedCalls);
        Assert.AreEqual(0, a.ClosedCalls, "Only the closed popup is notified");
        Assert.AreSame(a, _service.Top);
    }

    [Test]
    public void PopupClose_GoesThroughTheService()
    {
        CountingPopup popup = _service.Open(_prefabA);

        popup.Close();

        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void CloseAll_EmptiesTheStack()
    {
        _service.Open(_prefabA);
        _service.Open(_prefabB);

        _service.CloseAll();

        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void APopupDestroyedFromOutside_LeavesTheStack()
    {
        _service.Open(_prefabA);
        CountingPopup b = _service.Open(_prefabB);

        Object.DestroyImmediate(b.gameObject);

        Assert.AreEqual(1, _service.Count);
        Assert.AreEqual("PopupA", _service.Top.name);
    }

    [Test]
    public void ReopeningAfterClose_CreatesANewCopy()
    {
        CountingPopup first = _service.Open(_prefabA);
        first.Close();

        CountingPopup second = _service.Open(_prefabA);

        Assert.IsNotNull(second);
        Assert.AreEqual(1, _service.Count);
    }

    // ---------- Back button and scene changes ----------

    [Test]
    public void Back_ClosesOnlyThePopupOnTop()
    {
        CountingPopup settings = _service.Open(_prefabA);
        _service.Open(_prefabB);

        Assert.IsTrue(_service.HandleBack());

        Assert.AreEqual(1, _service.Count);
        Assert.AreSame(settings, _service.Top);
    }

    [Test]
    public void Back_WithNothingOpen_DoesNothing()
    {
        Assert.IsFalse(_service.HandleBack());
    }

    [Test]
    public void Back_WhenTheOptionIsOff_KeepsThePopupsOpen()
    {
        PopupTestUtils.SetField(_service, "closeTopOnBack", false);
        _service.Open(_prefabA);

        Assert.IsFalse(_service.HandleBack());
        Assert.AreEqual(1, _service.Count);
    }

    [Test]
    public void SceneChange_ClosesEveryPopup()
    {
        _service.Open(_prefabA);
        _service.Open(_prefabB);

        _service.HandleSceneChange();

        Assert.AreEqual(0, _service.Count);
    }
}
