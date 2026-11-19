using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Offline-first remote settings, like the profile:
///   - At start: the last values saved on the device (Cached), or none (Defaults: the local config assets).
///   - FetchAsync: fresh values from the server replace them (Remote) and are saved for the next launch.
///   - Once the game uses the values (ValuesInUse, set by the Boot), a late answer is only saved, not applied.
/// </summary>
public sealed class RemoteConfigService : IRemoteConfigService
{
    private readonly IRemoteConfigBackend _backend;
    private readonly RemoteConfigCache _cache;
    private readonly Func<DateTime> _clock;
    private readonly Func<PlayerAttributes> _attributes;

    public RemoteValues Values { get; private set; }
    public RemoteConfigSource Source { get; private set; }
    public bool ValuesInUse { get; set; }

    /// <param name="backend">The server. Null: no download, only the device copy (or nothing).</param>
    /// <param name="cache">The device copy. Null: nothing is saved or read.</param>
    /// <param name="attributes">The player as the Game Overrides see it, read at each download. Null: a new player.</param>
    public RemoteConfigService(IRemoteConfigBackend backend, RemoteConfigCache cache, Func<DateTime> clock = null,
        Func<PlayerAttributes> attributes = null)
    {
        _backend = backend;
        _cache = cache;
        _clock = clock ?? (() => DateTime.UtcNow);
        _attributes = attributes ?? (() => PlayerAttributes.NewPlayer);

        RemoteValues saved = _cache?.Load() ?? RemoteValues.Empty;
        Values = saved;
        Source = saved.Count > 0 ? RemoteConfigSource.Cached : RemoteConfigSource.Defaults;
    }

    public async Task<bool> FetchAsync()
    {
        if (_backend == null) return false;

        try
        {
            RemoteValues fresh = await _backend.FetchAsync(_attributes()) ?? RemoteValues.Empty;
            _cache?.Save(fresh, _clock());

            if (ValuesInUse)
            {
                Debug.Log("[RemoteConfig] New values arrived late: saved for the next launch.");
                return true;
            }

            Values = fresh;
            Source = RemoteConfigSource.Remote;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteConfig] Using the {(Source == RemoteConfigSource.Cached ? "last downloaded" : "local")} values: {e.Message}");
            return false;
        }
    }
}
