using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    public int totalLevels = 5;
    private int playerLevel;
    public int CurrentLevelIndex;
    private BoardManager board;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // UserDataManager validator
        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("UserDataManager not found. Try playing from Boot scene. Loading level 1.");
            playerLevel = 1;
        }
        else
        {
            // Reading player level
            playerLevel = UserDataManager.Instance.Profile.CurrentLevel;
        }

        CurrentLevelIndex = playerLevel;

        board = FindFirstObjectByType<BoardManager>();
        if (board == null)
            Debug.LogWarning("BoardManager not found");
    }

    private void Start()
    {
        //Called on Start instead of Awake to wait for the BoardManager initialisation
        LoadCorrectLevel();
    }

    ///<summary> 
    ///Called by "Next Level" button TODO:Unify with RestartLevel()
    ///</summary>
    public void NextLevel()
    {
        ReloadScene();
    }

    ///<summary> Called by "Retry" TODO:Unify with NextLevel() button</summary>
    public void RestartLevel()
    {
        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadCorrectLevel()
    {
        //Will be different in case of choosing a random level (if user has gone beyond last designed level)
        int levelToLoad = playerLevel;      

        // Check if the player has gone beyond the last designed level
        if (playerLevel > totalLevels)
        {
            levelToLoad = Random.Range(1, totalLevels + 1);
            Debug.Log($"Level {playerLevel} dont exist. Loading random level: {levelToLoad}");
        }

        // Final level to load
        if (board != null)
            board.InitializeLevel(levelToLoad);
    }
}