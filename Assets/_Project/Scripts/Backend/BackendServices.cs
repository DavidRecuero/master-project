using UnityEngine;

/// <summary>
/// The backend objects created by the Boot, shared with the rest of the game
/// (Settings uses them to delete the account). They outlive the Boot scene.
/// Null when the cloud is off or the game didn't start from the Boot scene.
/// </summary>
public static class BackendServices
{
    public static IBackendAuthService Auth { get; private set; }
    public static ICloudSaveStore CloudStore { get; private set; }
    public static CloudProfileSync ProfileSync { get; private set; }

    public static void Set(IBackendAuthService auth, ICloudSaveStore cloudStore, CloudProfileSync profileSync)
    {
        Auth = auth;
        CloudStore = cloudStore;
        ProfileSync = profileSync;
    }

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => Set(null, null, null);
}
