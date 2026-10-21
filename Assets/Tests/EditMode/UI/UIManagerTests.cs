using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManagerTests
{
    private GameObject _gameObject;
    private UIManager _uiManager;
    private LevelPillView _levelPill;
    private TextMeshProUGUI _levelLabel;

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
        _levelLabel = new GameObject("LevelText").AddComponent<TextMeshProUGUI>();
        _levelPill = new GameObject("LevelPill").AddComponent<LevelPillView>();
        typeof(LevelPillView).GetField("label", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_levelPill, _levelLabel);
        _uiManager.levelPills = new[] { _levelPill };
        _uiManager.resultSubtitleText = new GameObject("Subtitle").AddComponent<TextMeshProUGUI>();
        _uiManager.resultWinArt = new GameObject("WinArt");
        _uiManager.resultLoseArt = new GameObject("LoseArt");
        _uiManager.resultDelay = 0f;   // Coroutines don't run in EditMode tests
        _uiManager.playButton = new GameObject("PlayButton").AddComponent<Button>();

        _uiManager.resultText.transform.SetParent(_gameObject.transform);
        _uiManager.playButtonText.transform.SetParent(_gameObject.transform);
        _levelLabel.transform.SetParent(_gameObject.transform);
        _levelPill.transform.SetParent(_gameObject.transform);
        _uiManager.resultSubtitleText.transform.SetParent(_gameObject.transform);
        _uiManager.resultWinArt.transform.SetParent(_gameObject.transform);
        _uiManager.resultLoseArt.transform.SetParent(_gameObject.transform);
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
    public void Initialize_SetsTheLevelOnEveryPill()
    {
        _uiManager.Initialize(5, null);

        Assert.AreEqual(5, _levelPill.Level);
        Assert.AreEqual(LevelPillView.Format("<size=62%>LEVEL</size> {0}", 5), _levelLabel.text);
    }

    [Test]
    public void OnLevelCleared_ShowsWinPanelWithCorrectText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelCleared();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreEqual(_uiManager.winTitleText, _uiManager.resultText.text);
        Assert.AreEqual(_uiManager.winSubtitleText, _uiManager.resultSubtitleText.text);
        Assert.IsTrue(_uiManager.resultWinArt.activeSelf, "Win art on");
        Assert.IsFalse(_uiManager.resultLoseArt.activeSelf, "Lose art off");
        Assert.AreEqual(_uiManager.nextLevelButtonLabel, _uiManager.playButtonText.text);
    }

    [Test]
    public void OnLevelFailed_ShowsLosePanelWithRetryText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelFailed();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreEqual(_uiManager.loseTitleText, _uiManager.resultText.text);
        Assert.AreEqual(_uiManager.loseSubtitleText, _uiManager.resultSubtitleText.text);
        Assert.IsFalse(_uiManager.resultWinArt.activeSelf, "Win art off");
        Assert.IsTrue(_uiManager.resultLoseArt.activeSelf, "Lose art on");
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

    [Test]
    public void Result_WaitsForTheDelay_BeforeShowingUp()
    {
        _uiManager.resultPanel.SetActive(false);
        _uiManager.resultDelay = 1f;

        GameEvents.TriggerLevelCleared();

        Assert.IsFalse(_uiManager.resultPanel.activeSelf, "The last match finishes before the popup appears");
    }
}
