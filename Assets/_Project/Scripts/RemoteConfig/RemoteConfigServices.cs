using UnityEngine;

/// <summary>
/// The remote settings shared by the whole game, set up by the Boot. Until then (or if the game didn't start from
/// the Boot, e.g. Play on the Level scene) it is a service with no values, so every reader uses its local asset.
/// </summary>
public static class RemoteConfigServices
{
    private static readonly IRemoteConfigService NoValues = new RemoteConfigService(null, null);
    private static IRemoteConfigService _current;

    public static IRemoteConfigService Current => _current ?? NoValues;

    /// <summary>The real service: Unity Remote Config, with the last values saved in PlayerPrefs.</summary>
    /// <param name="environmentId">Remote Config environment ID, or null for the environment of the sign-in.</param>
    /// <param name="attributes">The player for the Game Overrides (segments). Null: a new player.</param>
    public static IRemoteConfigService CreateDefault(string environmentId = null, System.Func<PlayerAttributes> attributes = null) =>
        new RemoteConfigService(new UgsRemoteConfigBackend(environmentId), new RemoteConfigCache(new PlayerPrefsStorageProvider()),
            null, attributes);

    /// <summary>Replaces the shared service (Boot, tests). Null goes back to the one with no values.</summary>
    public static void Set(IRemoteConfigService service) => _current = service;

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => Set(null);
}
