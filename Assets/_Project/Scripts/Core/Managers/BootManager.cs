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

    [Header("Remote Config (Unity Gaming Services)")]
    [Tooltip("Off: the game only uses its own config assets (LivesConfig, BoosterDefinition...). Kill switch for a broken remote config.")]
    [SerializeField] private bool remoteConfigEnabled = true;
    [Tooltip("Longest wait (seconds) for the remote settings. After that the game uses the last ones it downloaded (or its own assets).")]
    [SerializeField] private float remoteConfigTimeout = 3f;
    [Tooltip("Optional. Remote Config ID of the 'development' environment (Dashboard > Project settings > Environments). Empty: the environment of the sign-in.")]
    [SerializeField] private string developmentEnvironmentId = "";
    [Tooltip("Optional. Remote Config ID of the 'production' environment. Empty: the environment of the sign-in.")]
    [SerializeField] private string productionEnvironmentId = "";

    [Header("Remote content (Cloud Content Delivery)")]
    [Tooltip("Off: the game never asks CCD for new content and plays with the content in the build. Kill switch.")]
    [SerializeField] private bool contentUpdateEnabled = true;
    [Tooltip("Longest wait (seconds, counted from the start of the Boot) for new content before loading the levels. " +
             "After that the game goes on with what it has; a late download is used on the next launch.")]
    [SerializeField] private float contentUpdateTimeout = 6f;

    [Header("Privacy")]
    [Tooltip("ConsentPopup prefab. On the first launch (or after a new consent version) the menu doesn't load until the player answers it.")]
    [SerializeField] private ConsentPopup consentPopupPrefab;

    [Header("Analytics")]
    [Tooltip("Off: analytics never start, whatever the player answered. Kill switch for a broken analytics SDK.")]
    [SerializeField] private bool analyticsEnabled = true;

    private ISceneLoader _sceneLoader;
    private IAuthProvider _authProvider;
    private ILocalizationService _localization;

    private IBackendAuthService _backendAuth;
    private ICloudSaveStore _cloudStore;
    private IProfileRepository _profiles;

    private IRemoteConfigService _remoteConfig;

    private IContentUpdater _contentUpdater;
    private Task<ContentUpdateResult?> _contentUpdate;
    private float _contentDeadline;

    private IConsentService _consent;
    private IPopupService _popups;
    private IAnalyticsService _analytics;

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

    /// <summary>Optional injection of the remote content check (tests). Without it, Addressables with CCD.</summary>
    public void InitializeContent(IContentUpdater updater, bool enabled = true, float? timeout = null)
    {
        _contentUpdater = updater;
        contentUpdateEnabled = enabled;
        if (timeout.HasValue) contentUpdateTimeout = timeout.Value;
    }

    /// <summary>Optional injection of the remote settings (tests). Without it, Unity Remote Config with the copy saved on the device.</summary>
    public void InitializeRemoteConfig(IRemoteConfigService remoteConfig, bool enabled = true, float? timeout = null)
    {
        _remoteConfig = remoteConfig;
        remoteConfigEnabled = enabled;
        if (timeout.HasValue) remoteConfigTimeout = timeout.Value;
    }

    /// <summary>Optional injection of the privacy pieces (tests). Without it, the shared ConsentServices and the PopupService of the scene.</summary>
    public void InitializeConsent(IConsentService consent, IPopupService popups, ConsentPopup popupPrefab = null)
    {
        _consent = consent;
        _popups = popups;
        if (popupPrefab != null) consentPopupPrefab = popupPrefab;
    }

    /// <summary>Optional injection of the analytics service (tests). Without it, GameAnalytics behind the player's consent.</summary>
    public void InitializeAnalytics(IAnalyticsService analytics, bool enabled = true)
    {
        _analytics = analytics;
        analyticsEnabled = enabled;
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
        _consent ??= ConsentServices.Current;
        _popups ??= PopupService.Instance;

        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        Debug.Log("[BOOT] Init services...");

        // CCD needs no sign-in: the content check runs while the rest of the Boot signs in
        StartContentUpdate();

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

        // New levels published on CCD, before the levels are read
        yield return ContentStep();

        // Before Remote Config: the segments need to know where the level loop starts
        yield return LevelsStep();

        // After the sign-in (the request needs it) and before anything reads a tunable number
        yield return RemoteConfigStep();

        // The menu must open with its texts already translated (no flash of empty labels)
        float waitUntil = Time.realtimeSinceStartup + localizationTimeout;
        yield return new WaitUntil(() => localizationReady || Time.realtimeSinceStartup >= waitUntil);

        if (localizationReady)
            Debug.Log($"[BOOT] Localization ready (language: {_localization.CurrentLanguage}, automatic: {_localization.FollowsDevice})");
        else
            Debug.LogWarning("[BOOT] Localization not ready in time. Loading the menu anyway.");

        // Before the menu: nothing that needs consent may start without an answer
        yield return ConsentStep();

        // Right after the answer, so the session is counted from the start
        AnalyticsStep();

        Debug.Log("[BOOT] Everything ready, loading main menu...");

        // Loads the Main Menu scene
        _sceneLoader.LoadScene(1);
    }

    /// <summary>
    /// First launch (or a new consent version): shows the consent popup and waits for the answer. After the
    /// language tables, so it opens already translated. The sign-in and the cloud don't need consent, so they are
    /// done by then. Without a popup or a PopupService (tests, Boot not set up) it goes on without asking.
    /// </summary>
    private IEnumerator ConsentStep()
    {
        if (_consent == null || !_consent.NeedsPrompt) yield break;

        if (_popups == null || consentPopupPrefab == null)
        {
            Debug.LogWarning("[BOOT] Consent needed but no PopupService or ConsentPopup prefab. Loading the menu without asking.");
            yield break;
        }

        ConsentPopup popup = _popups.Open(consentPopupPrefab);
        if (popup == null) yield break;
        popup.Initialize(_consent);

        Debug.Log("[BOOT] Waiting for the privacy answer...");
        // Unity's == null: the popup is destroyed when it closes
        yield return new WaitUntil(() => popup == null || popup.Answered);
    }

    /// <summary>
    /// Shares the analytics service with the whole game and starts it: GameAnalytics starts now only if the player
    /// said yes, and from then on follows the consent (Settings). With no answer or a no, nothing is created.
    /// </summary>
    private void AnalyticsStep()
    {
        if (!analyticsEnabled)
        {
            Debug.Log("[BOOT] Analytics off (kill switch).");
            return;
        }

        if (!GameTuning.AnalyticsEnabled(RemoteConfigServices.Current.Values, true))
        {
            Debug.Log("[BOOT] Analytics off (remote kill switch).");
            return;
        }

        _analytics ??= AnalyticsServices.CreateDefault(_consent);
        AnalyticsServices.Set(_analytics);

        // Before Start: the first events of the session already carry the player's group
        RemoteValues remote = RemoteConfigServices.Current.Values;
        _analytics.SetSegment(PlayerSegments.Segment(remote), PlayerSegments.Experiment(remote), PlayerSegments.Lifecycle(remote));
        _analytics.Start();

        // Turns the game events (levels, coins, boosters, lives) into analytics events for the whole session
        AnalyticsEventsTracker.StartShared(_analytics);

        // Tells the analytics when the app goes to the background and comes back (sessions)
        if (Application.isPlaying) AnalyticsAppLifecycle.Ensure();
    }

    private void StartContentUpdate()
    {
        if (!contentUpdateEnabled)
        {
            Debug.Log("[BOOT] Remote content off (kill switch). Using the content in the build.");
            return;
        }

        _contentUpdater ??= new AddressablesContentUpdater();
        _contentDeadline = Time.realtimeSinceStartup + contentUpdateTimeout;
        _contentUpdate = SafeUpdateAsync(_contentUpdater);
    }

    // Never throws: no network or no bucket yet is a normal case (null = nothing new)
    private static async Task<ContentUpdateResult?> SafeUpdateAsync(IContentUpdater updater)
    {
        try
        {
            return await updater.UpdateAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Content] No content update now, using what the device has: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Waits for the content check started at the beginning of the Boot, at most until contentUpdateTimeout
    /// seconds after that start. If it isn't done by then the Boot goes on: what it downloads stays in the
    /// Addressables cache and the game uses it on the next launch.
    /// </summary>
    private IEnumerator ContentStep()
    {
        if (_contentUpdate == null) yield break;

        yield return new WaitUntil(() => _contentUpdate.IsCompleted || Time.realtimeSinceStartup >= _contentDeadline);

        if (!_contentUpdate.IsCompleted)
            Debug.LogWarning($"[BOOT] Content check not done in {contentUpdateTimeout} s. Going on with the content on the device.");
        else if (_contentUpdate.Result.HasValue)
            Debug.Log($"[BOOT] Content up to date ({_contentUpdate.Result.Value})");
    }

    /// <summary>
    /// Loads every level from Addressables into memory, so the gameplay reads them synchronously. Local content:
    /// it takes a moment and never blocks the Boot (on failure the first level read tries again).
    /// </summary>
    private IEnumerator LevelsStep()
    {
        Task<LevelCatalog> load = LevelLibrary.LoadAsync();
        yield return new WaitUntil(() => load.IsCompleted);

        Debug.Log(LevelLibrary.IsLoaded
            ? $"[BOOT] Levels ready ({LevelLibrary.EnsureLoaded().Count})"
            : "[BOOT] Levels not loaded yet. The first level read will try again.");
    }

    /// <summary>
    /// Shares the remote settings with the whole game and tries to download fresh ones, at most remoteConfigTimeout
    /// seconds. Offline (or with the cloud off) it doesn't try: the last values saved on the device are used, or the
    /// local assets if there are none. Then the values are locked for the session (a late answer is only saved for
    /// the next launch) and applied to the systems that live in the Boot (lives, rewards). The rest (boosters,
    /// Rate Us) read them when their scene loads.
    /// </summary>
    private IEnumerator RemoteConfigStep()
    {
        if (!remoteConfigEnabled)
        {
            RemoteConfigServices.Set(null);
            Debug.Log("[BOOT] Remote config off (kill switch). Using the local config assets.");
            yield break;
        }

        // Remote Config rides on the UGS sign-in: with another backend (tests) there is no server to ask
        _remoteConfig ??= _backendAuth is UgsBackendAuthService
            ? RemoteConfigServices.CreateDefault(EnvironmentIdFor(BackendEnvironment.Current), CurrentPlayer)
            : new RemoteConfigService(null, null);
        RemoteConfigServices.Set(_remoteConfig);

        bool canFetch = cloudEnabled && _backendAuth != null && _backendAuth.IsSignedIn;
        if (canFetch)
        {
            float deadline = Time.realtimeSinceStartup + remoteConfigTimeout;
            Task<bool> fetch = _remoteConfig.FetchAsync();
            yield return new WaitUntil(() => fetch.IsCompleted || Time.realtimeSinceStartup >= deadline);

            if (!fetch.IsCompleted)
                Debug.LogWarning($"[BOOT] Remote config not ready in {remoteConfigTimeout} s. Going on without waiting more.");
        }

        // From now on the game uses these values
        _remoteConfig.ValuesInUse = true;

        RemoteValues values = _remoteConfig.Values;
        Debug.Log($"[BOOT] Remote config: {_remoteConfig.Source} ({values.Count} keys). Segment: {PlayerSegments.Segment(values)}, " +
                  $"experiment: {PlayerSegments.Experiment(values)}, lifecycle: {PlayerSegments.Lifecycle(values)}");

        // Unity's != : a destroyed UserDataManager must count as missing
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.ApplyRemoteConfig(values);

            // After the download: the server judged the activity of the previous launches, now this one counts
            UserDataManager.Instance.RegisterSession(System.DateTime.UtcNow);
        }
    }

    // The profile after the cloud sync, read when the download starts
    private static PlayerAttributes CurrentPlayer() =>
        UserDataManager.Instance != null
            ? PlayerAttributes.From(UserDataManager.Instance.Profile, System.DateTime.UtcNow, new LevelLoader().CountLevels())
            : PlayerAttributes.NewPlayer;

    private string EnvironmentIdFor(string environment)
    {
        string id = environment == BackendEnvironment.Production ? productionEnvironmentId : developmentEnvironmentId;
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
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
