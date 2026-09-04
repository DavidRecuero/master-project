using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    [Header("Level Settings")]
    public int totalLevels = 5;
    private int playerLevel;
    public int CurrentLevelIndex { get; private set; }
    private BoardManager board;

    private IUserDataProvider _userDataProvider;
    private ISceneLoader _sceneLoader;

    public void Initialize(IUserDataProvider userDataProvider, ISceneLoader sceneLoader)
    {
        _userDataProvider = userDataProvider;
        _sceneLoader = sceneLoader;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Assign default dependencies if not injected before
        _userDataProvider ??= UserDataManager.Instance;
        _sceneLoader ??= new UnitySceneLoader();

        // UserDataManager validator
        if (_userDataProvider == null)
        {
            Debug.LogWarning("UserDataManager not found. Try playing from Boot scene. Loading level 1.");
            playerLevel = 1;
        }
        else
        {
            // Reading player level
            playerLevel = _userDataProvider.CurrentLevel;
        }

        CurrentLevelIndex = playerLevel;

        board = FindFirstObjectByType<BoardManager>();
        if (board == null)
            Debug.LogWarning("BoardManager not found");

        CurrentState = GameState.Playing;
    }

    private void Start()
    {
        //Called on Start instead of Awake to wait for the BoardManager initialisation
        LoadCorrectLevel();
    }

    ///<summary> 
    ///Called by "Next Level"/"Play Again" buttons
    ///</summary>
    public void PlayAgain()
    {
        _sceneLoader.ReloadCurrentScene();
    }

    ///<summary>
    ///Called by "Got to Menu" button 
    ///</summary>
    public void BackToMenu()
    {
        //Back to Main Menu
        _sceneLoader.LoadScene(1);
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

    /// <summary>
    /// Tries to change the state. True if it was succesfully
    /// </summary>
    public bool TrySetState(GameState newState)
    {
        // If we go out of playing we block any other change
        if (CurrentState != GameState.Playing) return false;

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Victory:
                Debug.Log("[GAME STATE] VICTORY");
                GameEvents.TriggerLevelCleared();
                break;

            case GameState.Defeat:
                Debug.Log("[GAME STATE] DEFEAT");
                GameEvents.TriggerLevelFailed();
                break;
        }

        return true;
    }
}