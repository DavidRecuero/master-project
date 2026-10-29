using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class UIManager : MonoBehaviour
{
    [Header("Result Popup")]
    public GameObject resultPanel;
    [Tooltip("LocalizeStringEvent of the title. Leave its String Reference empty: it is set when the level ends.")]
    public LocalizeStringEvent resultTitleLabel;
    [Tooltip("Optional line under the title (LocalizeStringEvent, String Reference empty)")]
    public LocalizeStringEvent resultSubtitleLabel;
    [Tooltip("Shown only when the level is cleared (the three drops)")]
    public GameObject resultWinArt;
    [Tooltip("Shown only when the level is lost (the full grey tray)")]
    public GameObject resultLoseArt;
    [Tooltip("Seconds between the end of the level and the popup, so the last match can finish its animation")]
    public float resultDelay = 0.6f;

    [Header("Buttons")]
    public Button playButton;
    [Tooltip("LocalizeStringEvent of the button text (String Reference empty)")]
    public LocalizeStringEvent playButtonLabel;

    [Header("Indicators")]
    [Tooltip("Every LevelPill that shows the current level (gameplay HUD, result popup)")]
    public LevelPillView[] levelPills;

    [Header("Texts (UI table)")]
    [Tooltip("result.win_title")]
    public LocalizedString winTitle = new LocalizedString();
    [Tooltip("result.win_subtitle")]
    public LocalizedString winSubtitle = new LocalizedString();
    [Tooltip("result.next_level")]
    public LocalizedString nextLevelButton = new LocalizedString();
    [Tooltip("result.lose_title")]
    public LocalizedString loseTitle = new LocalizedString();
    [Tooltip("result.lose_subtitle")]
    public LocalizedString loseSubtitle = new LocalizedString();
    [Tooltip("result.retry")]
    public LocalizedString retryButton = new LocalizedString();

    private Action _onPlayAgainAction;
    private int _currentLevelIndex = 1;
    private Coroutine _resultRoutine;

    public void Initialize(int currentLevelIndex, Action onPlayAgainAction)
    {
        _currentLevelIndex = currentLevelIndex;
        _onPlayAgainAction = onPlayAgainAction;
        UpdateLevelIndicator();
    }

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += ShowWinScreen;
        GameEvents.OnLevelFailed += ShowLoseScreen;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= ShowWinScreen;
        GameEvents.OnLevelFailed -= ShowLoseScreen;
    }

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        // Get the current level from GameManager
        if (_onPlayAgainAction == null && GameManager.Instance != null)
        {
            _currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
            _onPlayAgainAction = GameManager.Instance.PlayAgain;
        }

        UpdateLevelIndicator();
    }

    private void UpdateLevelIndicator()
    {
        if (levelPills == null) return;

        foreach (LevelPillView pill in levelPills)
        {
            if (pill != null) pill.SetLevel(_currentLevelIndex);
        }
    }

    private void ShowWinScreen() => ShowResult(true);
    private void ShowLoseScreen() => ShowResult(false);

    private void ShowResult(bool won)
    {
        if (_resultRoutine != null) StopCoroutine(_resultRoutine);
        _resultRoutine = null;

        if (resultDelay > 0f && isActiveAndEnabled) _resultRoutine = StartCoroutine(ShowResultAfterDelay(won));
        else ApplyResult(won);
    }

    // Real time, so it also works if the game is paused at the end of the level
    private IEnumerator ShowResultAfterDelay(bool won)
    {
        yield return new WaitForSecondsRealtime(resultDelay);
        _resultRoutine = null;
        ApplyResult(won);
    }

    private void ApplyResult(bool won)
    {
        if (resultWinArt != null) resultWinArt.SetActive(won);
        if (resultLoseArt != null) resultLoseArt.SetActive(!won);

        if (won) SetupResultScreen(winTitle, winSubtitle, nextLevelButton, () => _onPlayAgainAction?.Invoke());
        else SetupResultScreen(loseTitle, loseSubtitle, retryButton, () => _onPlayAgainAction?.Invoke());
    }

    /// <summary>
    /// Configures the final panel, points its texts to the right entries of the UI table
    /// (the LocalizeStringEvents translate them and follow language changes) and assigns the button action
    /// </summary>
    private void SetupResultScreen(LocalizedString title, LocalizedString subtitle, LocalizedString buttonLabel, UnityEngine.Events.UnityAction buttonAction)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultTitleLabel != null) resultTitleLabel.StringReference = title;
        if (resultSubtitleLabel != null) resultSubtitleLabel.StringReference = subtitle;
        if (playButtonLabel != null) playButtonLabel.StringReference = buttonLabel;

        if (playButton != null)
        {
            // Cleans previous events to not accumulate calls and assings new behaviour
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(buttonAction);
        }
    }
}