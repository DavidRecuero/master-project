using System.Threading.Tasks;

/// <summary>
/// The game's connection to the cloud for the whole session (CloudSession).
/// The Boot connects first; the game can try again later (back from the background, before deleting the account).
/// </summary>
public interface ICloudConnection
{
    /// <summary>Signed in on the backend now.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Signs in and syncs the profile if that isn't done yet. True when signed in and synced.
    /// Safe to call often: does nothing when already connected, and joins an attempt in progress.
    /// Never throws.
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>Uploads the changes a failed upload left behind, if any. Never throws.</summary>
    Task FlushAsync();
}
