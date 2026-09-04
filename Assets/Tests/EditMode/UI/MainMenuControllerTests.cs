using NUnit.Framework;
using TMPro;
using UnityEngine;

public class MainMenuControllerTests
{
    private GameObject _gameObject;
    private MainMenuController _controller;
    private FakeUserDataProvider _userDataProvider;
    private FakeSceneLoader _sceneLoader;

    [SetUp]
    public void SetUp()
    {
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
}