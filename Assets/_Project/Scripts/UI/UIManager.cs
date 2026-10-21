using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Result Popup")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    [Tooltip("Optional line under the title")]
    public TextMeshProUGUI resultSubtitleText;
    [Tooltip("Shown only when the level is cleared (the three drops)")]
    public GameObject resultWinArt;
    [Tooltip("Shown only when the level is lost (the full grey tray)")]
    public GameObject resultLoseArt;
    [Tooltip("Seconds between the end of the level and the popup, so the last match can finish its animation")]
    public float resultDelay = 0.6f;

    [Header("Buttons")]
    public Button playButton;
    public TextMeshProUGUI playButtonText;

    [Header("Indicators")]
    [Tooltip("Every LevelPill that shows the current level (gameplay HUD, result popup)")]
    public LevelPillView[] levelPills;

    [Header("Text Configuration")]
    public string winTitleText = "LEVEL CLEARED!";
    public string loseTitleText = "OUT OF SPACE!";
    public string nextLevelButtonLabel = "NEXT LEVEL";
    public string retryButtonLabel = "RETRY";
    public string winSubtitleText = "Every pipe is empty. Great job!";
    public string loseSubtitleText = "The tray is full";

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

        if (won) SetupResultScreen(winTitleText, winSubtitleText, nextLevelButtonLabel, () => _onPlayAgainAction?.Invoke());
        else SetupResultScreen(loseTitleText, loseSubtitleText, retryButtonLabel, () => _onPlayAgainAction?.Invoke());
    }

    /// <summary>
    /// Configures the final panel, updates the text button and assigns its action dynamically
    /// </summary>
    private void SetupResultScreen(string title, string subtitle, string buttonLabel, UnityEngine.Events.UnityAction buttonAction)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = title;
        if (resultSubtitleText != null) resultSubtitleText.text = subtitle;
        if (playButtonText != null) playButtonText.text = buttonLabel;

        if (playButton != null)
        {
            // Cleans previous events to not accumulate calls and assings new behaviour
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(buttonAction);
        }
    }
}