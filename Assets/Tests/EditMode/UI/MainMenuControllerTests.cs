using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuControllerTests
{
    private GameObject _gameObject;
    private MainMenuController _controller;
    private FakeUserDataProvider _userDataProvider;
    private FakeSceneLoader _sceneLoader;
    private TextMeshProUGUI _coinsText;
    private LevelPillView _levelPill;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();

        _gameObject = new GameObject("MainMenuController");
        _controller = _gameObject.AddComponent<MainMenuController>();

        _userDataProvider = new FakeUserDataProvider();
        _sceneLoader = new FakeSceneLoader();

        // CoinsPill and LevelPill prefabs, reduced to their views and labels
        var coinsGo = new GameObject("CoinsPill");
        var pillGo = new GameObject("LevelPill");
        coinsGo.transform.SetParent(_gameObject.transform);
        pillGo.transform.SetParent(_gameObject.transform);

        _coinsText = new GameObject("Amount").AddComponent<TextMeshProUGUI>();
        _coinsText.transform.SetParent(coinsGo.transform);
        var coinsView = coinsGo.AddComponent<CoinsView>();
        typeof(CoinsView).GetField("amountText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(coinsView, _coinsText);

        _levelPill = pillGo.AddComponent<LevelPillView>();

        SetField("coinsView", coinsView);
        SetField("levelPill", _levelPill);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerSession.Reset();

        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Initialize_UpdatesCoinsAndLevelPill_Correctly()
    {
        _userDataProvider.Coins = 1250;
        _userDataProvider.CurrentLevel = 4;

        _controller.Initialize(_userDataProvider, _sceneLoader);

        Assert.AreEqual("1.250", _coinsText.text);
        Assert.AreEqual(4, _levelPill.Level);
    }

    [TestCase("Ana", 14, "Ana")]
    [TestCase("PipeMasterSupreme2000", 14, "PipeMasterS...")]
    [TestCase("", 14, "")]
    public void Truncate_CutsLongNamesWithAnEllipsis(string name, int max, string expected)
    {
        Assert.AreEqual(expected, MainMenuController.Truncate(name, max));
    }

    [Test]
    public void OnPlayButtonClicked_CallsLoadSceneWithCorrectIndex()
    {
        _controller.Initialize(_userDataProvider, _sceneLoader);

        _controller.OnPlayButtonClicked();

        Assert.AreEqual(2, _sceneLoader.LoadedSceneIndex);
    }

    private void SetField(string name, object value)
    {
        typeof(MainMenuController)
            .GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_controller, value);
    }

    [TestCase("Ana", "abc", "Ana")]
    [TestCase("", "1234567890", "Guest: 12345678")]
    [TestCase("", "short", "Guest: short")]
    [TestCase("", "", "Guest")]
    [TestCase(null, null, "Guest")]
    public void FormatPlayerLabel_ReturnsExpectedText(string displayName, string userId, string expected)
    {
        Assert.AreEqual(expected, MainMenuController.FormatPlayerLabel(displayName, userId));
    }

    [Test]
    public void Initialize_ShowsDisplayName_WhenSignedIn()
    {
        var go = new GameObject("PlayerIdText");
        go.transform.SetParent(_gameObject.transform);
        var text = go.AddComponent<TextMeshProUGUI>();
        SetField("playerIdText", text);
        PlayerSession.Set("Ana", "");
        _userDataProvider.UserId = "a_8473080197345671757";

        _controller.Initialize(_userDataProvider, _sceneLoader);

        Assert.AreEqual("Ana", text.text);
    }

    [Test]
    public void Initialize_ShowsGuestLabel_WhenThereIsNoDisplayName()
    {
        var go = new GameObject("PlayerIdText");
        go.transform.SetParent(_gameObject.transform);
        var text = go.AddComponent<TextMeshProUGUI>();
        SetField("playerIdText", text);
        _userDataProvider.UserId = "abcdefghijk";

        _controller.Initialize(_userDataProvider, _sceneLoader);

        Assert.AreEqual("Guest: abcdefgh", text.text);
    }

    [Test]
    public void Initialize_ShowsCachedAvatar_WhenAvailable()
    {
        var go = new GameObject("Avatar");
        go.transform.SetParent(_gameObject.transform);
        var image = go.AddComponent<RawImage>();
        go.SetActive(false);
        SetField("avatarImage", image);
        var texture = new Texture2D(2, 2);
        PlayerSession.Avatar = texture;

        _controller.Initialize(_userDataProvider, _sceneLoader);

        Assert.IsTrue(go.activeSelf);
        Assert.AreSame(texture, image.texture);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void Initialize_HidesAvatar_WhenThereIsNothingToShow()
    {
        var go = new GameObject("Avatar");
        go.transform.SetParent(_gameObject.transform);
        var image = go.AddComponent<RawImage>();
        SetField("avatarImage", image);

        _controller.Initialize(_userDataProvider, _sceneLoader);

        Assert.IsFalse(go.activeSelf);
    }
}