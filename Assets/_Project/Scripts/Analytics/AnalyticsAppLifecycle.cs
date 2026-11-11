using UnityEngine;

/// <summary>
/// Tells the analytics when the app goes to the background and comes back, so AnalyticsService can end and start
/// sessions itself (the provider's own session handling is off, see GameAnalyticsBackend). Created once by the Boot
/// and kept between scenes. Always talks to AnalyticsServices.Current, which decides with the player's consent.
/// </summary>
public class AnalyticsAppLifecycle : MonoBehaviour
{
    private static AnalyticsAppLifecycle _instance;

    /// <summary>Creates the object if it doesn't exist yet.</summary>
    public static void Ensure()
    {
        if (_instance != null) return;

        var go = new GameObject("AnalyticsAppLifecycle");
        _instance = go.AddComponent<AnalyticsAppLifecycle>();
        DontDestroyOnLoad(go);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        AnalyticsServices.Current.SetAppPaused(paused);
    }
}
