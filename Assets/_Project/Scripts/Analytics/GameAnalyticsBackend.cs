using GameAnalyticsSDK;
using UnityEngine;

/// <summary>
/// IAnalyticsBackend on the GameAnalytics SDK (8.x). The SDK never starts on its own: nothing is created or sent
/// until Initialize. Initialize creates the GameAnalytics object (it keeps itself between scenes and is what catches
/// the Unity errors for "Submit Unity errors automatically") and starts the SDK with the keys of Resources/GameAnalytics/Settings.
/// The keys are not in the repo: GameAnalyticsKeysBuildStep writes them into the settings only while building.
/// In the editor the SDK sends nothing (unsupported platform), so playing in the editor is safe.
///
/// Sessions: "Manual session handling" is on in the settings. Without it, the Android SDK opens a session (and
/// calls its server with the player ID and device data) every time the app comes back to the foreground, even with
/// the sending turned off. With it, only AnalyticsService starts and ends sessions, and only with consent.
/// Initialize still opens the first session.
///
/// Android detail: ending a session also stops the SDK's worker thread, and GameAnalytics.StartSession() only
/// queues its work on that thread, so on its own it never runs again. StartSession therefore calls the SDK's
/// own "app came back" step (native GameAnalytics.onResume: restarts the thread and opens the session), the same
/// thing the SDK does by itself without manual session handling, but now only when AnalyticsService says so.
/// </summary>
public sealed class GameAnalyticsBackend : IAnalyticsBackend
{
    public bool IsInitialized => GameAnalytics.Initialized;

    public void Initialize()
    {
        if (GameAnalytics.Initialized) return;

        if (Object.FindAnyObjectByType<GameAnalytics>() == null)
        {
            // GA_SpecialEvents comes with it (RequireComponent); its Awake does DontDestroyOnLoad
            new GameObject("GameAnalytics").AddComponent<GameAnalytics>();
        }

        GameAnalytics.Initialize();
        GameAnalytics.SetEnabledEventSubmission(true);
    }

    public void SetSubmissionEnabled(bool enabled)
    {
        if (!GameAnalytics.Initialized) return;

        // doCache false: while off, events are dropped, not stored to be sent later
        GameAnalytics.SetEnabledEventSubmission(enabled, false);
    }

    public void StartSession()
    {
        if (!GameAnalytics.Initialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // JNI reaches the package-private method; the SDK keeps its classes (-keep com.gameanalytics.sdk.**)
            using (var native = new AndroidJavaClass("com.gameanalytics.sdk.GameAnalytics"))
                native.CallStatic("onResume");
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Analytics] Native session resume failed, using StartSession: {e.Message}");
        }
#endif
        GameAnalytics.StartSession();
    }

    public void EndSession()
    {
        if (GameAnalytics.Initialized) GameAnalytics.EndSession();
    }

    public void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null)
    {
        GAProgressionStatus gaStatus = ToGa(status);

        if (string.IsNullOrEmpty(progression02))
        {
            if (score.HasValue) GameAnalytics.NewProgressionEvent(gaStatus, progression01, score.Value);
            else GameAnalytics.NewProgressionEvent(gaStatus, progression01);
        }
        else
        {
            if (score.HasValue) GameAnalytics.NewProgressionEvent(gaStatus, progression01, progression02, score.Value);
            else GameAnalytics.NewProgressionEvent(gaStatus, progression01, progression02);
        }
    }

    public void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId)
    {
        GAResourceFlowType gaFlow = flow == AnalyticsResourceFlow.Source ? GAResourceFlowType.Source : GAResourceFlowType.Sink;
        GameAnalytics.NewResourceEvent(gaFlow, currency, amount, itemType, itemId);
    }

    public void Design(string eventId, float? value = null)
    {
        if (value.HasValue) GameAnalytics.NewDesignEvent(eventId, value.Value);
        else GameAnalytics.NewDesignEvent(eventId);
    }

    private static GAProgressionStatus ToGa(AnalyticsProgressionStatus status)
    {
        switch (status)
        {
            case AnalyticsProgressionStatus.Start: return GAProgressionStatus.Start;
            case AnalyticsProgressionStatus.Complete: return GAProgressionStatus.Complete;
            default: return GAProgressionStatus.Fail;
        }
    }
}
