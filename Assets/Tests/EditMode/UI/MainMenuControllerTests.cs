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

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();

        _gameObject = new GameObject("MainMenuController");
        _controller = _gameObject.AddComponent<MainMenuController>();

        _userDataProvider = new FakeUserDataProvider();
        _sceneLoader = new FakeSceneLoader();

        var coinsGo = new GameObject("CoinsText");
        var playBtnGo = new GameObject("PlayButtonText");

        coinsGo.transform.SetParent(_gameObject.transform);
        playBtnGo.transform.SetParent(_gameObject.transform);

        var coinsText = coinsGo.AddComponent<TextMeshProUGUI>();
        var playButtonText = playBtnGo.AddComponent<TextMeshProUGUI>();

        typeof(MainMenuController)
            .GetField("coinsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_controller, coinsText);

        typeof(MainMenuController)
            .GetField("playButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_controller, playButtonText);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerSession.Reset();

        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Initialize_UpdatesCoinsAndLevelText_Correctly()
    {
        _userDataProvider.Coins = 250;
        _userDataProvider.CurrentLevel = 4;

        _controller.Initialize(_userDataProvider, _sceneLoader);

        var coinsText = (TextMeshProUGUI)typeof(MainMenuController)
            .GetField("coinsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_controller);

        var playBtnText = (TextMeshProUGUI)typeof(MainMenuController)
            .GetField("playButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_controller);

        Assert.AreEqual("250", coinsText.text);
        Assert.AreEqual("Level 4", playBtnText.text);
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