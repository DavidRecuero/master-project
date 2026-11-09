using UnityEngine;

public class GameManager : MonoBehaviour, IGameStateController, ILevelAbandoner
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    private int playerLevel;
    private bool isDebugOverride; // True when Tools > Level Loader forced the level (editor only)
    public int CurrentLevelIndex { get; private set; }

    [Header("Scene References")]

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private TrayManager trayManager;
    [SerializeField] private ItemPool itemPool;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private CoinsView coinsView;


    private IUserDataProvider _userDataProvider;
    private ISceneLoader _sceneLoader;
    private ILivesService _lives;

    /// <summary>True while this level holds a life: it is given back if the level is won.</summary>
    public bool LifeInPlay { get; private set; }

    public void Initialize(IUserDataProvider userDataProvider, ISceneLoader sceneLoader, ILivesService lives = null)
    {
        _userDataProvider = userDataProvider;
        _sceneLoader = sceneLoader;
        if (lives != null) _lives = lives;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Assign default dependencies if not injected before
        _userDataProvider ??= UserDataManager.Instance;
        _sceneLoader ??= new UnitySceneLoader();
        if (_lives == null && UserDataManager.Instance != null) _lives = UserDataManager.Instance.Lives;

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

#if UNITY_EDITOR
        // Tools > Level Loader can force a specific level while testing in the editor
        if (DebugLevelOverride.IsActive)
        {
            playerLevel = DebugLevelOverride.Level;
            isDebugOverride = true;
            Debug.Log($"🔧 [GameManager] Level Loader override: loading level {playerLevel}");
        }
#endif

        CurrentLevelIndex = playerLevel;
        CurrentState = GameState.Playing;
    }

    private void Start()
    {
        ICameraController camController = Camera.main != null ? Camera.main.GetComponent<ICameraController>() : null;
        ILevelLoader levelLoader = new LevelLoader();

        if (trayManager != null) trayManager.Initialize(itemPool, this, camController);
        if (boardManager != null) boardManager.Initialize(levelLoader, itemPool, camController);
        if (uiManager != null) uiManager.Initialize(CurrentLevelIndex, PlayAgain);
        if (boosterManager != null) boosterManager.Initialize(_userDataProvider as IBoosterInventory ?? UserDataManager.Instance, _userDataProvider);
        if (coinsView != null) coinsView.Initialize(_userDataProvider);

        TakeLevelLife();

        //Called on Start instead of Awake to wait for the BoardManager initialisation
        LoadCorrectLevel(levelLoader);
    }

    /// <summary>
    /// Starting the level uses a life (given back on Victory). Using it at the start, and not when the level
    /// is lost, is what makes closing the game mid-level cost the life too.
    /// The menus only open the level with a life (LivesGate); if there is none anyway, the level is played
    /// for free rather than blocked. Levels forced with Tools > Level Loader don't use lives.
    /// </summary>
    public bool TakeLevelLife()
    {
        if (LifeInPlay || isDebugOverride || _lives == null) return false;

        LifeInPlay = _lives.TryUseLife();
        if (!LifeInPlay) Debug.LogWarning("[GameManager] Level started with no lives left: it is played without using one.");
        return LifeInPlay;
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

    /// <summary>
    /// Called by "Abandon level" (gameplay Settings, after the confirmation).
    /// The level counts as lost: nothing is saved (no level, no coins; the boosters already used stay spent)
    /// and the player goes back to the main menu. Only while playing: once the level is won or lost it does nothing.
    /// </summary>
    public bool AbandonLevel()
    {
        if (!TrySetState(GameState.Abandoned)) return false;

        BackToMenu();
        return true;
    }

    private void LoadCorrectLevel(ILevelLoader levelLoader)
    {
        int levelToLoad = ResolveLevelToLoad(levelLoader);

        // Final level to load
        if (boardManager != null)
        {
            boardManager.InitializeLevel(levelToLoad);
        }
        else
        {
            Debug.LogError("❌ [GameManager] No BoardManager assigned to editor.");
        }
    }

    /// <summary>
    /// Level file to play for the player's level. Designed levels are played in order; past the last one,
    /// LevelSequence picks a board from the loop, always the same one for the same level number.
    /// </summary>
    private int ResolveLevelToLoad(ILevelLoader levelLoader)
    {
        // Tools > Level Loader plays exactly the file it was asked for
        if (isDebugOverride) return playerLevel;

        int designedLevels = levelLoader.CountLevels();
        if (designedLevels < 1)
        {
            Debug.LogError("❌ [GameManager] No level_XXX.json found in Resources.");
            return playerLevel;
        }

        LevelSequence sequence = new LevelSequence(designedLevels);
        int levelToLoad = sequence.ResolveLevelFile(playerLevel);

        if (sequence.IsLoopLevel(playerLevel))
        {
            Debug.Log($"[GameManager] Level {playerLevel} is past the last designed level ({designedLevels}). Loop board: level {levelToLoad}");
        }

        return levelToLoad;
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
                // Winning gives the life back (before the event, so the result screen already sees it)
                if (LifeInPlay) _lives.ReturnLife();
                LifeInPlay = false;
                GameEvents.TriggerLevelCleared();
                break;

            case GameState.Defeat:
                Debug.Log("[GAME STATE] DEFEAT");
                LifeInPlay = false;   // the life is lost
                GameEvents.TriggerLevelFailed();
                break;

            case GameState.Abandoned:
                Debug.Log("[GAME STATE] ABANDONED");
                LifeInPlay = false;   // abandoning counts as losing: the life is lost
                GameEvents.TriggerLevelAbandoned();
                break;
        }

        return true;
    }
}