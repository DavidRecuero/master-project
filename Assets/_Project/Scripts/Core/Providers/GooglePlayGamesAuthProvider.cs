using System;
using UnityEngine;
using UnityEngine.Scripting;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

/// <summary>
/// Real Google Play Games Services implementation of IAuthProvider
/// Only works on an Android device/build - never in the Editor.
/// Also hands out the one-time server code Unity Authentication needs (IServerAuthCodeProvider).
/// </summary>
public class GooglePlayGamesAuthProvider : IAuthProvider, IServerAuthCodeProvider
{
    public bool IsSignedIn { get; private set; }
    public string PlayerId { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public string AvatarUrl { get; private set; } = "";

    public GooglePlayGamesAuthProvider()
    {
        PlayGamesPlatform.Activate();
    }

    public void SignIn(Action<AuthResult> onComplete)
    {
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Finish(status, onComplete);
                return;
            }

            PlayGamesPlatform.Instance.ManuallyAuthenticate(status2 =>
                Finish(status2, onComplete));
        });
    }

    /// <summary>
    /// One-time code for Unity Authentication (needs the Web App Client ID in the plugin setup).
    /// forceRefreshToken: true, as Unity's sample does, so every start gets a fresh code.
    /// </summary>
    public void RequestServerAuthCode(Action<string> onCode)
    {
        if (!IsSignedIn)
        {
            onCode?.Invoke(null);
            return;
        }

        try
        {
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
            {
                if (string.IsNullOrEmpty(code)) Debug.LogWarning("[Auth] Play Games returned no server code.");
                onCode?.Invoke(code);
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auth] Could not request the server code: " + e.Message);
            onCode?.Invoke(null);
        }
    }

    private void Finish(SignInStatus status, Action<AuthResult> onComplete)
    {
        IsSignedIn = status == SignInStatus.Success;
        PlayerId = IsSignedIn ? PlayGamesPlatform.Instance.GetUserId() : "";
        DisplayName = IsSignedIn ? PlayGamesPlatform.Instance.GetUserDisplayName() : "";
        AvatarUrl = "";

        if (!IsSignedIn)
        {
            onComplete?.Invoke(MapStatus(status));
            return;
        }

        FetchAvatarUrl(() => onComplete?.Invoke(MapStatus(status)));
    }

    private static AuthResult MapStatus(SignInStatus status)
    {
        switch (status)
        {
            case SignInStatus.Success:
                return AuthResult.Success;
            case SignInStatus.Canceled:
                return AuthResult.Cancelled;
            default:
                return AuthResult.Error;
        }
    }

    private void FetchAvatarUrl(Action done)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var playGames = new AndroidJavaClass("com.google.android.gms.games.PlayGames"))
        using (var playersClient = playGames.CallStatic<AndroidJavaObject>("getPlayersClient", activity))
        using (var task = playersClient.Call<AndroidJavaObject>("getCurrentPlayer"))
        {
            task.Call<AndroidJavaObject>("addOnCompleteListener",
                new PlayerTaskListener(url =>
                {
                    AvatarUrl = url;
                    Debug.Log("[Auth] Avatar URL: " + (string.IsNullOrEmpty(url) ? "(none)" : url));
                    done();
                }));
            return;
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning("[Auth] Could not request player avatar: " + e.Message);
    }
#endif
        done();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
[Preserve]
private class PlayerTaskListener : AndroidJavaProxy
{
    private readonly Action<string> _onUrl;

    public PlayerTaskListener(Action<string> onUrl)
        : base("com.google.android.gms.tasks.OnCompleteListener")
    {
        _onUrl = onUrl;
    }

    [Preserve]
    public void onComplete(AndroidJavaObject task)
    {
        string url = "";
        try
        {
            if (task.Call<bool>("isSuccessful"))
            {
                using (var player = task.Call<AndroidJavaObject>("getResult"))
                {
                    url = player.Call<string>("getHiResImageUrl");
                    if (string.IsNullOrEmpty(url))
                        url = player.Call<string>("getIconImageUrl");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auth] Avatar URL read failed: " + e.Message);
        }

        _onUrl(url ?? "");
    }
}
#endif
}