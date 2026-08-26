using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    public int totalLevels = 3;
    private static int currentLevelIndex = 1; // Persistent level counter

    [Header("UI Elements")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject nextLevelButton;
    public GameObject restartButton;
    public TextMeshProUGUI levelIndicatorText;

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += HandleLevelCleared;
        GameEvents.OnLevelFailed += HandleLevelFailed;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= HandleLevelCleared;
        GameEvents.OnLevelFailed -= HandleLevelFailed;
    }

    private void HandleLevelCleared() => GameOver(true);
    private void HandleLevelFailed() => GameOver(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();
        if (board != null)
            board.InitializeLevel(currentLevelIndex);

        if (levelIndicatorText != null)
            levelIndicatorText.text = $"LEVEL {currentLevelIndex}";
    }

    public void GameOver(bool hasWon)
    {
        if (resultPanel) resultPanel.SetActive(true);

        if (hasWon)
        {
            if (resultText != null) resultText.text = "LEVEL CLEARED!";
            if (nextLevelButton) nextLevelButton.SetActive(true);
            if (restartButton) restartButton.SetActive(false);
        }
        else
        {
            if (resultText != null) resultText.text = "GAME OVER!";
            if (nextLevelButton) nextLevelButton.SetActive(false);
            if (restartButton) restartButton.SetActive(true);
        }
    }

    // Called by "Next Level" button
    public void NextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex > totalLevels)
        {
            currentLevelIndex = 1; // Loop back to Level 1
        }
        ReloadScene();
    }

    // Called by "Retry" button
    public void RestartLevel()
    {
        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}