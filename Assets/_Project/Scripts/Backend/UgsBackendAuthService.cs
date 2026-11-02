using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

/// <summary>
/// IBackendAuthService on top of Unity Authentication.
///
/// Play Games players always sign in with their Play Games code, never anonymously: if the code
/// exchange fails, this throws and the game stays offline for the session (no orphan guest accounts).
/// Guests sign in anonymously; Unity caches the session on the device, so they get the same account
/// on every start until they uninstall.
/// </summary>
public class UgsBackendAuthService : IBackendAuthService
{
    public bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;

    public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : "";

    public async Task InitializeAsync(string environment)
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return;

        InitializationOptions options = new InitializationOptions().SetEnvironmentName(environment);
        await UnityServices.InitializeAsync(options);
    }

    public async Task SignInAsync(string playGamesAuthCode)
    {
        if (AuthenticationService.Instance.IsSignedIn) return;

        if (!string.IsNullOrEmpty(playGamesAuthCode))
        {
            // Signs in the account linked to this Play Games player, or creates it. Replaces any cached session.
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(playGamesAuthCode);
            return;
        }

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}
