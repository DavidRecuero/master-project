using UnityEngine;

/// <summary>
/// Entry point of haptics. Lives in the Boot scene and survives scene loads, like the AudioManager.
/// It builds the HapticsService with the backend of the platform (native vibrator on an Android device,
/// a do-nothing backend in the Editor) and the saved on/off choice, and exposes it as IHapticsService.
/// </summary>
public class HapticsManager : MonoBehaviour, IHapticsService
{
    public static HapticsManager Instance { get; private set; }

    [Tooltip("Don't vibrate when the phone's own Touch feedback setting is off. Off by default: many players turn " +
             "that off for the keyboard, and the game has its own Vibration switch in Settings")]
    [SerializeField] private bool respectSystemTouchFeedback;
    [Tooltip("Log every vibration (console in the Editor, logcat on the device)")]
    [SerializeField] private bool logVibrations;

    private IHapticsService _service;
    private AndroidHapticsBackend _androidBackend;

    /// <summary>Optional injection (tests, or a different backend / storage).</summary>
    public void Initialize(IHapticsBackend backend, HapticsSettingsStore settings = null)
    {
        _service = new HapticsService(backend, settings, () => Time.unscaledTime);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        if (_service == null)
        {
            var settings = new HapticsSettingsStore(new PlayerPrefsStorageProvider());
            _service = new HapticsService(CreateBackend(), settings, () => Time.unscaledTime);
        }
    }

    private IHapticsBackend CreateBackend()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            _androidBackend = new AndroidHapticsBackend(respectSystemTouchFeedback, logVibrations);
            return _androidBackend;
        }

        // Editor and other platforms: "supported" in the Editor so the Settings row can be seen and tested
        return new NullHapticsBackend(Application.isEditor, logVibrations);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // The player may have changed the phone's Touch feedback while the game was in the background
        if (hasFocus) _androidBackend?.RefreshSystemSetting();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        _androidBackend?.Dispose();
        _androidBackend = null;
    }

    // ---------- IHapticsService ----------

    public bool Enabled
    {
        get => _service != null && _service.Enabled;
        set { if (_service != null) _service.Enabled = value; }
    }

    public bool IsSupported => _service != null && _service.IsSupported;

    public void Play(HapticType type) => _service?.Play(type);
}
