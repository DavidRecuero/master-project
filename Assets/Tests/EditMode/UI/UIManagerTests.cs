using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManagerTests
{
    private GameObject _gameObject;
    private UIManager _uiManager;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("UIManager");
        _uiManager = _gameObject.AddComponent<UIManager>();

        var panel = new GameObject("ResultPanel");
        panel.transform.SetParent(_gameObject.transform);
        _uiManager.resultPanel = panel;

        _uiManager.resultText = new GameObject("ResultText").AddComponent<TextMeshProUGUI>();
        _uiManager.playButtonText = new GameObject("ButtonText").AddComponent<TextMeshProUGUI>();
        _uiManager.levelIndicatorText = new GameObject("LevelText").AddComponent<TextMeshProUGUI>();
        _uiManager.playButton = new GameObject("PlayButton").AddComponent<Button>();

        _uiManager.resultText.transform.SetParent(_gameObject.transform);
        _uiManager.playButtonText.transform.SetParent(_gameObject.transform);
        _uiManager.levelIndicatorText.transform.SetParent(_gameObject.transform);
        _uiManager.playButton.transform.SetParent(_gameObject.transform);

        typeof(UIManager)
            .GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_uiManager, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (_uiManager != null)
        {
            typeof(UIManager)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(_uiManager, null);
        }

        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Initialize_SetsLevelIndicatorText_Correctly()
    {
        _uiManager.Initialize(5, null);

        Assert.AreEqual("LEVEL 5", _uiManager.levelIndicatorText.text);
    }

    [Test]
    public void OnLevelCleared_ShowsWinPanelWithCorrectText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelCleared();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreEqual(_uiManager.winTitleText, _uiManager.resultText.text);
        Assert.AreEqual(_uiManager.nextLevelButtonLabel, _uiManager.playButtonText.text);
    }

    [Test]
    public void OnLevelFailed_ShowsLosePanelWithRetryText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelFailed();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreEqual(_uiManager.loseTitleText, _uiManager.resultText.text);
        Assert.AreEqual(_uiManager.retryButtonLabel, _uiManager.playButtonText.text);
    }

    [Test]
    public void PlayButton_InvokesCallback_OnClicked()
    {
        bool actionCalled = false;
        _uiManager.Initialize(1, () => actionCalled = true);

        GameEvents.TriggerLevelCleared();
        _uiManager.playButton.onClick.Invoke();

        Assert.IsTrue(actionCalled);
    }
}