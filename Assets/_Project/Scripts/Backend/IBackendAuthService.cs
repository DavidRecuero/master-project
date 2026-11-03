using System.Threading.Tasks;

/// <summary>
/// The player's account on the backend (Unity Gaming Services).
/// Different from IAuthProvider: that one is the platform identity (Google Play Games, guest),
/// this one is the account the cloud data belongs to.
/// Methods throw on failure (no network, service down): the caller decides how to fall back.
/// </summary>
public interface IBackendAuthService
{
    bool IsSignedIn { get; }

    /// <summary>The backend player ID, or an empty string when not signed in.</summary>
    string PlayerId { get; }

    /// <summary>Starts the services on the given environment ("development" / "production").</summary>
    Task InitializeAsync(string environment);

    /// <summary>
    /// With a Play Games code: signs in the account linked to that Play Games player (created on first use).
    /// Without it: anonymous sign-in, which reuses the account cached on this device.
    /// </summary>
    Task SignInAsync(string playGamesAuthCode);

    /// <summary>
    /// Deletes the signed-in account on the backend for good and forgets its session on this device.
    /// The next sign-in creates a new account.
    /// </summary>
    Task DeleteAccountAsync();
}
