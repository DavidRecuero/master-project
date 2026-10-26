using UnityEngine;

/// <summary>
/// Scene glue for GameplayAudioRouter: put it in the Level scene. It builds the router with the
/// AudioManager and the tray, and hooks it to GameEvents while the scene is alive.
/// </summary>
public class GameplayAudio : MonoBehaviour
{
    [SerializeField] private TrayManager trayManager;

    [Header("Tuning")]
    [Tooltip("Free tray slots at or below this play the warning")]
    public int warningFreeSlots = 2;
    [Tooltip("Seconds after a match in which the next one counts as a combo (higher pitch)")]
    public float comboWindow = 2.5f;
    [Tooltip("Highest combo step. Each step is a whole tone higher.")]
    public int comboMaxSteps = 4;
    [Tooltip("Seconds the music stays lowered under the win/lose jingle")]
    public float jingleDuckSeconds = 3f;

    private IAudioService _audio;
    private IUserDataProvider _userData;
    private GameplayAudioRouter _router;

    /// <summary>Optional injection for tests or a different setup.</summary>
    public void Initialize(IAudioService audio, IUserDataProvider userData = null)
    {
        _audio = audio;
        _userData = userData;
    }

    private void Awake()
    {
        if (trayManager == null) trayManager = FindFirstObjectByType<TrayManager>();
        _audio ??= AudioManager.Instance;
        _userData ??= UserDataManager.Instance;

        if (_audio == null) Debug.LogWarning("[GameplayAudio] No AudioManager found. Play from the Boot scene to hear sounds.");
    }

    private void OnEnable()
    {
        if (_audio == null) return;

        _router = new GameplayAudioRouter(
            _audio,
            () => Time.unscaledTime,
            trayManager != null ? () => trayManager.maxCapacity - trayManager.TrayItems.Count : (System.Func<int>)null,
            _userData != null ? _userData.Coins : 0,
            new ComboTracker(comboWindow, comboMaxSteps))
        {
            warningFreeSlots = warningFreeSlots,
            jingleDuckSeconds = jingleDuckSeconds
        };
        _router.Subscribe();
    }

    private void OnDisable()
    {
        _router?.Unsubscribe();
        _router = null;
    }
}
