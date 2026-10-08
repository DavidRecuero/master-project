using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    private ISceneLoader _sceneLoader;
    private IAuthProvider _authProvider;

    public void Initialize(ISceneLoader sceneLoader, IAuthProvider authProvider)
    {
        _sceneLoader = sceneLoader;
        _authProvider = authProvider;
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
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        Debug.Log("[BOOT] Init services...");

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

        Debug.Log("[BOOT] Everything ready, loading main menu...");

        // Loads the Main Menu scene 
        _sceneLoader.LoadScene(1);
    }
}