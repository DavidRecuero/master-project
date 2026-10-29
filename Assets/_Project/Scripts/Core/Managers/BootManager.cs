using System.Collections;
using UnityEngine;

public class BootManager : MonoBehaviour
{
    [Tooltip("If the language tables take longer than this (seconds), the game goes on without waiting more")]
    [SerializeField] private float localizationTimeout = 10f;

    private ISceneLoader _sceneLoader;
    private IAuthProvider _authProvider;
    private ILocalizationService _localization;

    public void Initialize(ISceneLoader sceneLoader, IAuthProvider authProvider, ILocalizationService localization = null)
    {
        _sceneLoader = sceneLoader;
        _authProvider = authProvider;
        if (localization != null) _localization = localization;
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

        if (_authProvider.IsSignedIn && UserDataManager.Instance != null)
        {
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
}
