using System.Threading.Tasks;

/// <summary>
/// The server that sends the settings (Unity Remote Config). Only the download: the device copy and the rules on
/// when values apply are in RemoteConfigService, so they are tested without the network.
/// </summary>
public interface IRemoteConfigBackend
{
    /// <summary>
    /// The settings for this player, as the server resolves them (defaults + the Game Overrides whose conditions
    /// match these attributes). Throws if there is no fresh answer (no network, not signed in, service down).
    /// </summary>
    Task<RemoteValues> FetchAsync(PlayerAttributes attributes);
}
