using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class BootManager : MonoBehaviour
{
    [Tooltip("If the language tables take longer than this (seconds), the game goes on without waiting more")]
    [SerializeField] private float localizationTimeout = 10f;

    [Header("Cloud (Unity Gaming Services)")]
    [Tooltip("Off: no cloud at all, the game only uses the data on the device (as in 0.2.x). Kill switch for a broken backend.")]
    [SerializeField] private bool cloudEnabled = true;
    [Tooltip("Longest wait (seconds) for the cloud sign-in and the save download. After that the menu opens with the data on the device.")]
    [SerializeField] private float cloudTimeout = 5f;

    private ISceneLoader _sceneLoader;
    private IAuthProvider _authProvider;
    private ILocalizationService _localization;

    private IBackendAuthService _backendAuth;
    private ICloudSaveStore _cloudStore;
    private IProfileRepository _profiles;

    public void Initialize(ISceneLoader sceneLoader, IAuthProvider authProvider, ILocalizationService localization = null)
    {
        _sceneLoader = sceneLoader;
        _authProvider = authProvider;
        if (localization != null) _localization = localization;
    }

    /// <summary>
    /// Optional injection of the cloud pieces (tests). Without it: Unity Authentication, Cloud Save and the
    /// UserDataManager. With no profile repository (no UserDataManager in the scene) the cloud step is skipped.
    /// </summary>
    public void InitializeCloud(IBackendAuthService backendAuth, ICloudSaveStore cloudStore, IProfileRepository profiles,
        float? timeout = null, bool enabled = true)
    {
        _backendAuth = backendAuth;
        _cloudStore = cloudStore;
        _profiles = profiles;
        if (timeout.HasValue) cloudTimeout = timeout.Value;
        cloudEnabled = enabled;
    }

    private void Awake()
    {
        _sceneLoader ??= new UnitySceneLoader();

#if UNITY_ANDROID && !UNITY_EDITOR
        _authProvider ??= new GooglePlayGamesAuthProvider();
#else
        _authProvider ??= new GuestAuthProvider();
#endif
    }

    private void Start()
    {
        // Resolved here and not in Awake, so a test can inject a fake right after AddComponent
        _localization ??= UnityLocalizationService.Instance;
        // Unity's != (not ??=): a destroyed UserDataManager must count as missing
        if (_profiles == null && UserDataManager.Instance != null) _profiles = UserDataManager.Instance;
        _backendAuth ??= new UgsBackendAuthService();
        _cloudStore ??= new UgsCloudSaveStore();

        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        Debug.Log("[BOOT] Init services...");

        // Language tables and sign-in load at the same time
        bool localizationReady = false;
        _localization.Initialize(() => localizationReady = true);

        bool signInComplete = false;
        AuthResult signInResult = AuthResult.Error;

        _authProvider.SignIn(result =>
        {
            signInResult = result;
            signInComplete = true;
        });

        yield return new WaitUntil(() => signInComplete);

        Debug.Log($"[BOOT] Sign-in result: {signInResult} (PlayerId: {_authProvider.PlayerId})");

        if (_authProvider.IsSignedIn)
            PlayerSession.Set(_authProvider.DisplayName, _authProvider.AvatarUrl);

        if (cloudEnabled && _profiles != null)
        {
            yield return CloudStep();
        }
        else if (_authProvider.IsSignedIn && UserDataManager.Instance != null)
        {
            // Cloud off: the platform ID identifies the profile, as before the cloud existed
            UserDataManager.Instance.SetUserId(_authProvider.PlayerId);
        }

        // The menu must open with its texts already translated (no flash of empty labels)
        float waitUntil = Time.realtimeSinceStartup + localizationTimeout;
        yield return new WaitUntil(() => localizationReady || Time.realtimeSinceStartup >= waitUntil);

        if (localizationReady)
            Debug.Log($"[BOOT] Localization ready (language: {_localization.CurrentLanguage}, automatic: {_localization.FollowsDevice})");
        else
            Debug.LogWarning("[BOOT] Localization not ready in time. Loading the menu anyway.");

        Debug.Log("[BOOT] Everything ready, loading main menu...");

        // Loads the Main Menu scene
        _sceneLoader.LoadScene(1);
    }

    /// <summary>
    /// Creates the cloud session for the whole game and waits for its first connection, at most cloudTimeout
    /// seconds. Whatever happens, the menu then opens with the profile in use locked (a late or later cloud copy
    /// never replaces it mid-session), and the CloudReconnector keeps trying when the game comes back from the
    /// background. See CloudSession for the sign-in rules (Play Games code, guests, never a guest for Play Games).
    /// </summary>
    private IEnumerator CloudStep()
    {
        var session = new CloudSession(_authProvider, _backendAuth, _cloudStore, _profiles, BackendEnvironment.Current, cloudTimeout);
        BackendServices.Set(_backendAuth, _cloudStore, session);
        if (Application.isPlaying) CloudReconnector.Watch(session);

        float deadline = Time.realtimeSinceStartup + cloudTimeout;
        Task<bool> connection = session.ConnectAsync();
        yield return new WaitUntil(() => connection.IsCompleted || Time.realtimeSinceStartup >= deadline);

        // From now on the player is using this profile
        session.Sync.ProfileInUse = true;

        if (!connection.IsCompleted)
            Debug.LogWarning($"[BOOT] Cloud not ready in {cloudTimeout} s. Opening the menu with the data on this device.");
        else if (connection.Result)
            Debug.Log($"[BOOT] Cloud ready (PlayerId: {_backendAuth.PlayerId})");
        else
            Debug.Log("[BOOT] Cloud unavailable. Using the data on this device; it will retry when the game comes back from the background.");
    }
}
