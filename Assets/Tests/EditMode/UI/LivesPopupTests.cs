using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>The lives popup: one heart per life, status line, and the refill button with its price.</summary>
public class LivesPopupTests
{
    private LivesPopup _popup;
    private FakeLivesService _lives;
    private Image _heart;
    private Sprite _full;
    private Sprite _empty;
    private LocalizeStringEvent _status;
    private Button _refill;
    private LocalizeStringEvent _refillLabel;
    private GameObject _notEnoughCoins;

    [SetUp]
    public void SetUp()
    {
        _popup = PopupTestUtils.MakePrefab<LivesPopup>("LivesPopup");

        var row = new GameObject("Hearts", typeof(RectTransform));
        row.transform.SetParent(_popup.transform, false);
        _heart = new GameObject("Heart", typeof(RectTransform)).AddComponent<Image>();
        _heart.transform.SetParent(row.transform, false);

        _full = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        _empty = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);

        _status = Label("Status");
        _refill = new GameObject("RefillButton", typeof(RectTransform)).AddComponent<Button>();
        _refill.transform.SetParent(_popup.transform, false);
        _refillLabel = Label("RefillLabel");
        _notEnoughCoins = new GameObject("NotEnoughCoins");
        _notEnoughCoins.transform.SetParent(_popup.transform, false);

        PopupTestUtils.SetField(_popup, "heartTemplate", _heart);
        PopupTestUtils.SetField(_popup, "fullHeart", _full);
        PopupTestUtils.SetField(_popup, "emptyHeart", _empty);
        PopupTestUtils.SetField(_popup, "statusLabel", _status);
        PopupTestUtils.SetField(_popup, "refillButton", _refill);
        PopupTestUtils.SetField(_popup, "refillLabel", _refillLabel);
        PopupTestUtils.SetField(_popup, "notEnoughCoins", _notEnoughCoins);

        _lives = new FakeLivesService();
    }

    private LocalizeStringEvent Label(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_popup.transform, false);
        go.AddComponent<TextMeshProUGUI>();
        return go.AddComponent<LocalizeStringEvent>();
    }

    private LocalizedString Text(string field) =>
        (LocalizedString)typeof(LivesPopup).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_popup);

    [TearDown]
    public void TearDown()
    {
        if (_popup != null) Object.DestroyImmediate(_popup.gameObject);
        Object.DestroyImmediate(_full);
        Object.DestroyImmediate(_empty);
    }

    [Test]
    public void ShowsOneHeartPerLifeOfTheMaximum()
    {
        _popup.Initialize(_lives);

        Assert.AreEqual(5, _popup.Hearts.Count);
        Assert.AreEqual(5, _heart.transform.parent.childCount);
    }

    [Test]
    public void Hearts_AreFullForTheLivesThePlayerHas_AndEmptyForTheRest()
    {
        _lives.Lives = 3;

        _popup.Initialize(_lives);

        for (int i = 0; i < 5; i++)
            Assert.AreSame(i < 3 ? _full : _empty, _popup.Hearts[i].sprite, $"Heart {i + 1}");
    }

    [Test]
    public void Full_SaysSo_AndHidesTheRefill()
    {
        _popup.Initialize(_lives);

        Assert.AreSame(Text("fullText"), _status.StringReference);
        Assert.IsFalse(_refill.gameObject.activeSelf);
        Assert.IsFalse(_notEnoughCoins.activeSelf);
    }

    [Test]
    public void MissingLives_ShowsTheCountdownAndTheRefillPrice()
    {
        _lives.Lives = 2;   // 3 missing x 20

        _popup.Initialize(_lives);

        Assert.AreSame(Text("nextLifeText"), _status.StringReference);
        Assert.AreEqual("12:34", Text("nextLifeText").GetVariableValue(LivesPopup.TimeVariable));
        Assert.IsTrue(_refill.gameObject.activeSelf);
        Assert.IsTrue(_refill.interactable);
        Assert.AreSame(Text("refillText"), _refillLabel.StringReference);
        Assert.AreEqual(60, Text("refillText").GetVariableValue(LivesPopup.PriceVariable));
    }

    [Test]
    public void NotEnoughCoins_DisablesTheRefillAndSaysWhy()
    {
        _lives.Lives = 0;   // 100 coins needed
        _lives.Coins = 99;

        _popup.Initialize(_lives);

        Assert.IsFalse(_refill.interactable);
        Assert.IsTrue(_notEnoughCoins.activeSelf);
    }

    [Test]
    public void Refill_FillsEveryLife_AndTheHeartsFollow()
    {
        _lives.Lives = 1;
        _popup.Initialize(_lives);

        _popup.OnRefillClicked();

        Assert.AreEqual(1, _lives.RefillCalls);
        Assert.AreEqual(5, _lives.Lives);
        Assert.AreSame(_full, _popup.Hearts[4].sprite);
        Assert.IsFalse(_refill.gameObject.activeSelf);
    }

    [Test]
    public void LifeComingBack_UpdatesThePopup()
    {
        _lives.Lives = 4;
        _popup.Initialize(_lives);

        _lives.ReturnLife();   // raises LivesChanged

        Assert.AreSame(_full, _popup.Hearts[4].sprite);
        Assert.AreSame(Text("fullText"), _status.StringReference);
    }

    [Test]
    public void SmallerMaximum_HidesTheExtraHearts()
    {
        _popup.Initialize(_lives);
        _lives.MaxLives = 3;
        _lives.Lives = 3;

        _popup.Refresh();

        Assert.AreEqual(3, _popup.Hearts.Count);
    }
}
