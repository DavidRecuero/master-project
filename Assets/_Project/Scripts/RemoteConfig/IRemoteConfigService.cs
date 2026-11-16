using System.Threading.Tasks;

/// <summary>
/// The remote settings the game uses in this session. The Boot downloads them once; from then on the values don't
/// change until the next launch (a price or a number of lives never changes in the middle of a level or a popup).
/// </summary>
public interface IRemoteConfigService
{
    /// <summary>The values in use. Empty when there is nothing remote: every reader falls back to its local asset.</summary>
    RemoteValues Values { get; }

    RemoteConfigSource Source { get; }

    /// <summary>
    /// True once the game started with the current values (set by the Boot). A download that finishes after that
    /// is only saved on the device for the next launch.
    /// </summary>
    bool ValuesInUse { get; set; }

    /// <summary>Downloads the settings. True if fresh values arrived. Never throws.</summary>
    Task<bool> FetchAsync();
}
