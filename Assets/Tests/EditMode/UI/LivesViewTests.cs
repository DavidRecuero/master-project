using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>The lives pill of the main menu: number, heart, "Full" or countdown, and the popup on tap.</summary>
public class LivesViewTests
{
    private GameObject _go;
    private LivesView _view;
    private FakeLivesService _lives;
    private Image _heart;
    private TextMeshProUGUI _count;
    private TextMeshProUGUI _status;
    private LocalizeStringEvent _statusLabel;
    private Sprite _full;
    private Sprite _empty;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("LivesPill", typeof(RectTransform));
        _view = _go.AddComponent<LivesView>();

        _heart = new GameObject("Heart", typeof(RectTransform)).AddComponent<Image>();
        _heart.transform.SetParent(_go.transform, false);
        _count = new GameObject("Count", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _count.transform.SetParent(_heart.transform, false);
        var statusGo = new GameObject("Status", typeof(RectTransform));
        statusGo.transform.SetParent(_go.transform, false);
        _status = statusGo.AddComponent<TextMeshProUGUI>();
        _statusLabel = statusGo.AddComponent<LocalizeStringEvent>();

        _full = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        _empty = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);

        PopupTestUtils.SetField(_view, "heartImage", _heart);
        PopupTestUtils.SetField(_view, "fullHeart", _full);
        PopupTestUtils.SetField(_view, "emptyHeart", _empty);
        PopupTestUtils.SetField(_view, "countText", _count);
        PopupTestUtils.SetField(_view, "statusText", _status);
        PopupTestUtils.SetField(_view, "statusLabel", _statusLabel);

        _lives = new FakeLivesService();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_full);
        Object.DestroyImmediate(_empty);
    }

    private LocalizedString FullText =>
        (LocalizedString)typeof(LivesView).GetField("fullText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_view);

    [Test]
    public void Full_ShowsTheNumberAndTheFullLabel()
    {
        _view.Initialize(_lives);

        Assert.AreEqual("5", _count.text);
        Assert.AreSame(_full, _heart.sprite);
        Assert.IsTrue(_statusLabel.enabled);
        Assert.AreSame(FullText, _statusLabel.StringReference);
    }

    [Test]
    public void MissingLives_ShowTheCountdown()
    {
        _lives.Lives = 3;

        _view.Initialize(_lives);

        Assert.AreEqual("3", _count.text);
        Assert.IsFalse(_statusLabel.enabled, "The translated label must not write over the countdown");
        Assert.AreEqual("12:34", _status.text);
    }

    [Test]
    public void NoLives_ShowsTheEmptyHeart()
    {
        _lives.Lives = 0;

        _view.Initialize(_lives);

        Assert.AreEqual("0", _count.text);
        Assert.AreSame(_empty, _heart.sprite);
    }

    [Test]
    public void FollowsTheChanges()
    {
        _view.Initialize(_lives);

        _lives.TryUseLife();

        Assert.AreEqual("4", _count.text);
    }

    [Test]
    public void Tap_OpensTheLivesPopup()
    {
        var popups = new FakePopupService();
        LivesPopup prefab = PopupTestUtils.MakePrefab<LivesPopup>("LivesPopup");
        PopupTestUtils.SetField(_view, "livesPopupPrefab", prefab);
        _view.Initialize(_lives, popups);

        _view.OnClicked();

        Assert.AreSame(prefab, popups.Top);
        Object.DestroyImmediate(prefab.gameObject);
    }
}
