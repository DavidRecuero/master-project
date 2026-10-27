using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PopupCloseButtonTests
{
    private GameObject _serviceGo;
    private PopupService _service;
    private CountingPopup _prefab;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();

        // A popup with the round X button somewhere inside its card
        _prefab = PopupTestUtils.MakePrefab<CountingPopup>("Popup");
        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(_prefab.transform);
        var close = new GameObject("CloseButton", typeof(RectTransform), typeof(Button));
        close.transform.SetParent(card.transform);
        close.AddComponent<PopupCloseButton>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefab.gameObject);
    }

    [Test]
    public void CloseButton_ClosesThePopupItIsIn()
    {
        CountingPopup popup = _service.Open(_prefab);

        popup.GetComponentInChildren<PopupCloseButton>().CloseParentPopup();

        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void CloseButton_InTheTopPopup_LeavesThePopupBelowOpen()
    {
        CountingPopup below = PopupTestUtils.MakePrefab<CountingPopup>("Below");
        try
        {
            CountingPopup belowInstance = _service.Open(below);
            CountingPopup top = _service.Open(_prefab);

            top.GetComponentInChildren<PopupCloseButton>().CloseParentPopup();

            Assert.AreEqual(1, _service.Count);
            Assert.AreSame(belowInstance, _service.Top);
        }
        finally
        {
            Object.DestroyImmediate(below.gameObject);
        }
    }

    [Test]
    public void CloseButton_OutsideAPopup_Warns()
    {
        var loose = new GameObject("CloseButton", typeof(RectTransform), typeof(Button));
        try
        {
            var button = loose.AddComponent<PopupCloseButton>();
            LogAssert.Expect(LogType.Warning, "[PopupCloseButton] Not inside a popup.");

            button.CloseParentPopup();
        }
        finally
        {
            Object.DestroyImmediate(loose);
        }
    }
}
