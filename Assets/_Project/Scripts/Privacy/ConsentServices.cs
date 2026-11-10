using UnityEngine;

/// <summary>
/// The consent service shared by the whole game (consent popup, Settings, analytics). Created on first use with
/// the device storage, so it works from any scene and before the sign-in. Tests replace it with Set.
/// </summary>
public static class ConsentServices
{
    private static IConsentService _current;

    public static IConsentService Current =>
        _current ??= new ConsentService(new ConsentStore(new PlayerPrefsStorageProvider()), new SystemClock());

    /// <summary>Replaces the shared service (tests). Null goes back to the real one on the next use.</summary>
    public static void Set(IConsentService service) => _current = service;

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => _current = null;
}
