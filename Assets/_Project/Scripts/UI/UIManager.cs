using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Buttons")]
    public Button playButton;
    public TextMeshProUGUI playButtonText;

    [Header("Indicators")]
    public TextMeshProUGUI levelIndicatorText;

    [Header("Text Configuration")]
    public string winTitleText = "LEVEL CLEARED!";
    public string loseTitleText = "GAME OVER!";
    public string nextLevelButtonLabel = "NEXT LEVEL";
    public string retryButtonLabel = "RETRY";
    public string levelPrefix = "LEVEL ";

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
        if (levelIndicatorText != null)
            levelIndicatorText.text = $"{levelPrefix}{GameManager.Instance.CurrentLevelIndex}";
    }

    private void ShowWinScreen()
    {
        SetupResultScreen(winTitleText, nextLevelButtonLabel, GameManager.Instance.PlayAgain);
    }

    private void ShowLoseScreen()
    {
        SetupResultScreen(loseTitleText, retryButtonLabel, GameManager.Instance.PlayAgain);
    }

    /// <summary>
    /// Configures the final panel, updates the text button and assigns its action dynamically
    /// </summary>
    private void SetupResultScreen(string title, string buttonLabel, UnityEngine.Events.UnityAction buttonAction)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = title;
        if (playButtonText != null) playButtonText.text = buttonLabel;

        if (playButton != null)
        {
            // Cleans previous events to not accumulate calls and assings new behaviour
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(buttonAction);
        }
    }
}