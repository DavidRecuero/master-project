using UnityEngine;

/// <summary>
/// The analytics service shared by the whole game, set up by the Boot. Until then (or if the game didn't start from
/// the Boot) it is a service that sends nothing, so the game can always call AnalyticsServices.Current safely.
/// </summary>
public static class AnalyticsServices
{
    private static readonly IAnalyticsService Silent = new AnalyticsService(null, null);
    private static IAnalyticsService _current;

    public static IAnalyticsService Current => _current ?? Silent;

    /// <summary>The real service: GameAnalytics behind the player's consent.</summary>
    public static IAnalyticsService CreateDefault(IConsentService consent) =>
        new AnalyticsService(consent ?? ConsentServices.Current, new GameAnalyticsBackend());

    /// <summary>Replaces the shared service (Boot, tests). Null goes back to the silent one.</summary>
    public static void Set(IAnalyticsService service)
    {
        if (_current != null && !ReferenceEquals(_current, service)) _current.Stop();
        _current = service;
    }

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => Set(null);
}
