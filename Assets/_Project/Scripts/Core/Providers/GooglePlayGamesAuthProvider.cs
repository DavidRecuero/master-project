using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

/// <summary>
/// Real Google Play Games Services implementation of IAuthProvider
/// Only works on an Android device/build - never in the Editor.
/// </summary>
public class GooglePlayGamesAuthProvider : IAuthProvider
{
    public bool IsSignedIn { get; private set; }
    public string PlayerId { get; private set; } = "";
    public string DisplayName { get; private set; } = "";

    public GooglePlayGamesAuthProvider()
    {
        PlayGamesPlatform.Activate();
    }

    public void SignIn(Action<AuthResult> onComplete)
    {
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            IsSignedIn = status == SignInStatus.Success;
            PlayerId = IsSignedIn ? PlayGamesPlatform.Instance.GetUserId() : "";
            DisplayName = IsSignedIn ? PlayGamesPlatform.Instance.GetUserDisplayName() : "";

            onComplete?.Invoke(MapStatus(status));
        });
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
}