using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Keeps the profile on the device and its copy in the cloud equal.
///
/// Offline-first: the device copy is the one the game uses. The cloud is a backup that
/// follows the player to other devices or reinstalls.
///
///   - Boot (PullAsync): downloads the cloud copy, picks the winner (ProfileMerger) and leaves both equal.
///   - After that, every save on the device is uploaded. Saves that arrive during an upload are
///     grouped into one more upload (no timers, never more than one request at a time).
///   - Nothing is uploaded before a successful download, so a device that couldn't read the
///     cloud never overwrites it.
///   - A failed upload is not retried right away: the next save, or the next boot, uploads it.
/// </summary>
public class CloudProfileSync
{
    public const string ProfileKey = "profile";

    private readonly ICloudSaveStore _store;
    private readonly IProfileRepository _profiles;

    private bool _uploading;
    private bool _uploadAgain;

    /// <summary>True once the boot download went well: from then on, saves are uploaded.</summary>
    public bool CanUpload { get; private set; }

    /// <summary>True while stopped (the account is being deleted): nothing is uploaded.</summary>
    public bool IsStopped { get; private set; }

    /// <summary>True when the last upload failed (the cloud is behind the device until the next one works).</summary>
    public bool HasPendingChanges { get; private set; }

    /// <summary>
    /// True once the player is using the profile on the device (the Boot sets it when it opens the menu).
    /// From then on, a cloud copy that is ahead is not applied over it (it would change the game mid-session)
    /// and nothing is uploaded, so the cloud keeps it for the next start. Applies to a slow boot download
    /// and to a reconnection later in the session.
    /// </summary>
    public bool ProfileInUse { get; set; }

    public CloudProfileSync(ICloudSaveStore store, IProfileRepository profiles)
    {
        _store = store;
        _profiles = profiles;
    }

    /// <summary>
    /// Boot sync for the signed-in backend player. Throws if the cloud can't be read
    /// (the game then goes on with the device copy and uploads nothing this session).
    /// </summary>
    public async Task<ProfileSource> PullAsync(string playerId)
    {
        string json = await _store.LoadAsync(ProfileKey);
        UserProfile cloud = Parse(json);
        ProfileSource winner = ProfileMerger.Choose(_profiles.Profile, cloud);

        if (winner == ProfileSource.Cloud)
        {
            if (ProfileInUse)
            {
                Debug.LogWarning("[Cloud] The cloud copy is ahead, but the game is already using the device copy. It will be used on the next start.");
                return winner;
            }

            cloud.UserId = playerId;
            _profiles.ReplaceProfile(cloud);
            StartUploading();
            Debug.Log($"[Cloud] Took the cloud copy (level {cloud.CurrentLevel}).");
            return winner;
        }

        _profiles.SetOwner(playerId);
        StartUploading();
        await UploadAsync();
        Debug.Log($"[Cloud] Uploaded the device copy (level {_profiles.Profile.CurrentLevel}).");
        return winner;
    }

    /// <summary>Stops the uploads (account deletion: the saves that follow must not recreate the cloud copy).</summary>
    public void Stop() => IsStopped = true;

    /// <summary>Uploads again, starting with the current device copy (the account deletion failed).</summary>
    public async Task ResumeAsync()
    {
        IsStopped = false;
        await UploadAsync();
    }

    /// <summary>Uploads the device copy now. Does nothing before a successful PullAsync or while stopped. Never throws.</summary>
    public async Task UploadAsync()
    {
        if (!CanUpload || IsStopped) return;

        if (_uploading)
        {
            _uploadAgain = true;
            return;
        }

        _uploading = true;
        try
        {
            do
            {
                _uploadAgain = false;
                await _store.SaveAsync(ProfileKey, JsonUtility.ToJson(_profiles.Profile));
                HasPendingChanges = false;
            }
            while (_uploadAgain);
        }
        catch (Exception e)
        {
            HasPendingChanges = true;
            Debug.LogWarning($"[Cloud] Upload failed, it will be retried on the next save: {e.Message}");
        }
        finally
        {
            _uploading = false;
        }
    }

    private void StartUploading()
    {
        if (CanUpload) return;
        CanUpload = true;
        _profiles.ProfileSaved += OnProfileSaved;
    }

    // async void only here, as an event handler: UploadAsync never throws
    private async void OnProfileSaved() => await UploadAsync();

    /// <summary>The cloud copy, or null when there is none or it can't be read.</summary>
    public static UserProfile Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonUtility.FromJson<UserProfile>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] Unreadable cloud profile, the device copy is kept: {e.Message}");
            return null;
        }
    }
}
