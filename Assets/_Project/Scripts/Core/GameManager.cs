using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    public int totalLevels = 3;
    private static int currentLevelIndex = 1; // Persistent level counter

    public static int CurrentLevelIndex => currentLevelIndex;

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
    }

    ///<summary> 
    ///Called by "Next Level" button
    ///</summary>
    public void NextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex > totalLevels)
        {
            currentLevelIndex = 1; // Loop back to Level 1
        }
        ReloadScene();
    }

    ///<summary> Called by "Retry" button</summary>
    public void RestartLevel()
    {
        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}