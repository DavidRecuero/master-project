using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>The "out of lives" popup: countdown, buy a life and play, or play for free once a life is back.</summary>
public class NoLivesPopupTests
{
    private GameObject _serviceGo;
    private PopupService _service;
    private NoLivesPopup _prefab;
    private FakeLivesService _lives;
    private int _played;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();
        _prefab = PopupTestUtils.MakePrefab<NoLivesPopup>("NoLivesPopup");
        _lives = new FakeLivesService { Lives = 0 };
        _played = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefab.gameObject);
    }

    /// <summary>Opened through the service, with its labels and button, as LivesGate does.</summary>
    private (NoLivesPopup popup, Button button, LocalizeStringEvent action, LocalizeStringEvent status, GameObject coin, GameObject hint) Open()
    {
        NoLivesPopup popup = _service.Open(_prefab);
        var button = new GameObject("BuyButton", typeof(RectTransform)).AddComponent<Button>();
        button.transform.SetParent(popup.transform, false);
        LocalizeStringEvent action = Label(popup, "Label");
        LocalizeStringEvent status = Label(popup, "Status");
        var coin = new GameObject("Coin");
        coin.transform.SetParent(popup.transform, false);
        var hint = new GameObject("NotEnoughCoins");
        hint.transform.SetParent(popup.transform, false);

        PopupTestUtils.SetField(popup, "actionButton", button);
        PopupTestUtils.SetField(popup, "actionLabel", action);
        PopupTestUtils.SetField(popup, "statusLabel", status);
        PopupTestUtils.SetField(popup, "coinIcon", coin);
        PopupTestUtils.SetField(popup, "notEnoughCoins", hint);

        popup.Initialize(_lives, () => _played++);
        return (popup, button, action, status, coin, hint);
    }

    private static LocalizeStringEvent Label(Component parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<TextMeshProUGUI>();
        return go.AddComponent<LocalizeStringEvent>();
    }

    private static LocalizedString Text(NoLivesPopup popup, string field) =>
        (LocalizedString)typeof(NoLivesPopup).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(popup);

    [Test]
    public void NoLives_ShowsTheCountdownAndTheLifePrice()
    {
        var ui = Open();

        Assert.AreSame(Text(ui.popup, "nextLifeText"), ui.status.StringReference);
        Assert.AreEqual("12:34", Text(ui.popup, "nextLifeText").GetVariableValue(NoLivesPopup.TimeVariable));
        Assert.AreSame(Text(ui.popup, "buyText"), ui.action.StringReference);
        Assert.AreEqual(20, Text(ui.popup, "buyText").GetVariableValue(NoLivesPopup.PriceVariable));
        Assert.IsTrue(ui.coin.activeSelf);
        Assert.IsTrue(ui.button.interactable);
        Assert.IsFalse(ui.hint.activeSelf);
    }

    [Test]
    public void Buying_PaysClosesAndPlays()
    {
        var ui = Open();

        ui.popup.OnActionClicked();

        Assert.AreEqual(1, _lives.BuyCalls);
        Assert.AreEqual(80, _lives.Coins);
        Assert.AreEqual(1, _played);
        Assert.AreEqual(0, _service.Count, "The popup closes before the level loads");
    }

    [Test]
    public void NotEnoughCoins_DisablesTheButtonAndPlaysNothing()
    {
        _lives.Coins = 5;
        var ui = Open();

        Assert.IsFalse(ui.button.interactable);
        Assert.IsTrue(ui.hint.activeSelf);

        ui.popup.OnActionClicked();

        Assert.AreEqual(0, _played);
        Assert.AreEqual(1, _service.Count);
    }

    [Test]
    public void LifeComingBack_TurnsTheButtonIntoAFreePlay()
    {
        var ui = Open();

        _lives.ReturnLife();   // a life regenerated while the popup was open

        Assert.IsTrue(ui.popup.LifeReady);
        Assert.AreSame(Text(ui.popup, "playText"), ui.action.StringReference);
        Assert.AreSame(Text(ui.popup, "readyText"), ui.status.StringReference);
        Assert.IsFalse(ui.coin.activeSelf);

        ui.popup.OnActionClicked();

        Assert.AreEqual(0, _lives.BuyCalls, "Nothing is bought");
        Assert.AreEqual(1, _played);
    }

    [Test]
    public void Closing_LeavesThePlayerWhereTheyWere()
    {
        var ui = Open();

        ui.popup.Close();

        Assert.AreEqual(0, _played);
        Assert.AreEqual(0, _lives.BuyCalls);
        Assert.AreEqual(0, _lives.Listeners, "Stops listening once closed");
    }

    [Test]
    public void DoubleTap_PlaysOnce()
    {
        _lives.Lives = 1;
        var ui = Open();

        ui.popup.OnActionClicked();
        if (ui.popup != null) ui.popup.OnActionClicked();

        Assert.AreEqual(1, _played);
    }
}
