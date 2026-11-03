using System;
using System.Threading.Tasks;
using UnityEngine;

public enum AccountDeletionResult
{
    /// <summary>Account and data deleted, on the backend and on the device.</summary>
    Deleted,

    /// <summary>Not signed in on the backend this session (no connection at boot): nothing was deleted.</summary>
    NotSignedIn,

    /// <summary>The backend failed (connection lost, service down): nothing was deleted on the device.</summary>
    Failed
}

/// <summary>Deletes the player's account. Interface so the popup can be tested with a fake.</summary>
public interface IAccountDeletion
{
    /// <summary>False when the game is offline this session: the account can't be deleted now.</summary>
    bool IsAvailable { get; }

    Task<AccountDeletionResult> DeleteAsync();
}

/// <summary>
/// "Delete account" (required by Google Play for apps that create accounts), in order:
///   1. Stop the cloud uploads, so the next saves don't recreate the data.
///   2. Delete the cloud profile (Cloud Save).
///   3. Delete the account (Unity Authentication). Unity also deletes the player's data in its services.
///   4. Reset the profile on the device.
/// If the backend fails, the device keeps everything and the uploads start again: the player can retry.
/// After Deleted, the game restarts from Boot, which signs in to a new, empty account.
/// </summary>
public class AccountDeletion : IAccountDeletion
{
    private readonly IBackendAuthService _auth;
    private readonly ICloudSaveStore _store;
    private readonly CloudProfileSync _sync;
    private readonly IUserDataProvider _profiles;

    public AccountDeletion(IBackendAuthService auth, ICloudSaveStore store, CloudProfileSync sync, IUserDataProvider profiles)
    {
        _auth = auth;
        _store = store;
        _sync = sync;
        _profiles = profiles;
    }

    /// <summary>The services the Boot created, or null pieces if the cloud is off.</summary>
    public static AccountDeletion FromBackendServices(IUserDataProvider profiles) =>
        new AccountDeletion(BackendServices.Auth, BackendServices.CloudStore, BackendServices.ProfileSync, profiles);

    public bool IsAvailable => _auth != null && _store != null && _auth.IsSignedIn;

    public async Task<AccountDeletionResult> DeleteAsync()
    {
        if (!IsAvailable) return AccountDeletionResult.NotSignedIn;

        _sync?.Stop();

        try
        {
            await DeleteCloudProfile();
            await _auth.DeleteAccountAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Account] Deletion failed, nothing deleted on the device: {e.Message}");
            if (_sync != null) await _sync.ResumeAsync();
            return AccountDeletionResult.Failed;
        }

        _profiles?.ResetData();
        Debug.Log("[Account] Account deleted. Restarting with a new one.");
        return AccountDeletionResult.Deleted;
    }

    // Unity deletes the player's Cloud Save data with the account; this makes it explicit and immediate.
    // A failure here doesn't stop the deletion (e.g. the key was never uploaded).
    private async Task DeleteCloudProfile()
    {
        try
        {
            await _store.DeleteAsync(CloudProfileSync.ProfileKey);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Account] Cloud profile not deleted (Unity deletes it with the account): {e.Message}");
        }
    }
}
