using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class UIManagerTests
{
    private GameObject _gameObject;
    private UIManager _uiManager;
    private LevelPillView _levelPill;
    private LocalizeStringEvent _levelLabel;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("UIManager");
        _uiManager = _gameObject.AddComponent<UIManager>();

        var panel = new GameObject("ResultPanel");
        panel.transform.SetParent(_gameObject.transform);
        _uiManager.resultPanel = panel;

        _uiManager.resultTitleLabel = AddLabel("ResultText");
        _uiManager.playButtonLabel = AddLabel("ButtonText");
        _levelLabel = AddLabel("LevelText");
        _levelPill = new GameObject("LevelPill").AddComponent<LevelPillView>();
        typeof(LevelPillView).GetField("label", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_levelPill, _levelLabel);
        _uiManager.levelPills = new[] { _levelPill };
        _uiManager.resultSubtitleLabel = AddLabel("Subtitle");
        _uiManager.resultWinArt = new GameObject("WinArt");
        _uiManager.resultLoseArt = new GameObject("LoseArt");
        _uiManager.resultDelay = 0f;   // Coroutines don't run in EditMode tests
        _uiManager.playButton = new GameObject("PlayButton").AddComponent<Button>();

        _levelPill.transform.SetParent(_gameObject.transform);
        _uiManager.resultWinArt.transform.SetParent(_gameObject.transform);
        _uiManager.resultLoseArt.transform.SetParent(_gameObject.transform);
        _uiManager.playButton.transform.SetParent(_gameObject.transform);

        typeof(UIManager)
            .GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_uiManager, null);
    }

    /// <summary>A TMP text with its LocalizeStringEvent, as in the ResultPopup prefab.</summary>
    private LocalizeStringEvent AddLabel(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_gameObject.transform);
        go.AddComponent<TextMeshProUGUI>();
        return go.AddComponent<LocalizeStringEvent>();
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
        Assert.AreEqual(5, _levelLabel.StringReference.GetVariableValue(LevelPillView.LevelVariable));
    }

    [Test]
    public void OnLevelCleared_ShowsWinPanelWithCorrectText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelCleared();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreSame(_uiManager.winTitle, _uiManager.resultTitleLabel.StringReference);
        Assert.AreSame(_uiManager.winSubtitle, _uiManager.resultSubtitleLabel.StringReference);
        Assert.IsTrue(_uiManager.resultWinArt.activeSelf, "Win art on");
        Assert.IsFalse(_uiManager.resultLoseArt.activeSelf, "Lose art off");
        Assert.AreSame(_uiManager.nextLevelButton, _uiManager.playButtonLabel.StringReference);
    }

    [Test]
    public void OnLevelFailed_ShowsLosePanelWithRetryText()
    {
        _uiManager.Initialize(1, null);

        GameEvents.TriggerLevelFailed();

        Assert.IsTrue(_uiManager.resultPanel.activeSelf);
        Assert.AreSame(_uiManager.loseTitle, _uiManager.resultTitleLabel.StringReference);
        Assert.AreSame(_uiManager.loseSubtitle, _uiManager.resultSubtitleLabel.StringReference);
        Assert.IsFalse(_uiManager.resultWinArt.activeSelf, "Win art off");
        Assert.IsTrue(_uiManager.resultLoseArt.activeSelf, "Lose art on");
        Assert.AreSame(_uiManager.retryButton, _uiManager.playButtonLabel.StringReference);
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
