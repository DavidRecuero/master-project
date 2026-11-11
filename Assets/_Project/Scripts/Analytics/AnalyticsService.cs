using UnityEngine;

/// <summary>
/// Analytics only with consent (GDPR opt-in). Pure C#: the consent and the provider come from outside.
///   - Start with a yes: the provider starts.
///   - A yes later (Settings): the provider starts then, or, if it already ran this session, sending goes back on.
///   - The yes is withdrawn: sending goes off at once and the events of the session are dropped, not kept.
///   - No answer or a no: the provider is never started, nothing is created and nothing is sent.
///   - Sessions are driven from here (the provider's own handling is off): the session ends when the app goes to
///     the background or the yes is withdrawn, and a new one only starts with a yes. Without this, the provider
///     would contact its server every time the app comes back, even after the yes was withdrawn.
/// Every event call checks IsActive, so the game can call it from anywhere.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IConsentService _consent;
    private readonly IAnalyticsBackend _backend;
    private bool _started;
    private bool _sending;
    private bool _paused;

    public AnalyticsService(IConsentService consent, IAnalyticsBackend backend)
    {
        _consent = consent;
        _backend = backend;
    }

    public bool IsActive => _sending && _backend != null && _backend.IsInitialized;

    public void Start()
    {
        if (_started) return;
        _started = true;

        GameEvents.OnConsentChanged += OnConsentChanged;
        Apply(_consent != null && _consent.IsGranted(ConsentPurpose.Analytics));
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;
        GameEvents.OnConsentChanged -= OnConsentChanged;
    }

    private void OnConsentChanged(ConsentPurpose purpose, bool granted)
    {
        if (purpose == ConsentPurpose.Analytics) Apply(granted);
    }

    private void Apply(bool granted)
    {
        if (_backend == null) return;

        if (granted)
        {
            if (!_backend.IsInitialized)
            {
                _backend.Initialize();
                Debug.Log("[Analytics] Started (consent given)");
            }
            else
            {
                _backend.SetSubmissionEnabled(true);
                if (!_paused) _backend.StartSession();
                Debug.Log("[Analytics] Sending again (consent given)");
            }
            _sending = true;
        }
        else
        {
            // Never started: nothing to stop
            if (_backend.IsInitialized)
            {
                // Sending off first: the end of the session is not sent after the no
                _backend.SetSubmissionEnabled(false);
                _backend.EndSession();
                Debug.Log("[Analytics] Sending stopped (consent withdrawn)");
            }
            _sending = false;
        }
    }

    public void SetAppPaused(bool paused)
    {
        if (_paused == paused) return;
        _paused = paused;

        if (_backend == null || !_backend.IsInitialized) return;

        if (paused)
        {
            if (_sending) _backend.EndSession();
        }
        else if (_sending)
        {
            // Back from the background: a new session only with the yes still in place
            _backend.StartSession();
        }
    }

    public void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null)
    {
        if (!IsActive || string.IsNullOrEmpty(progression01)) return;
        _backend.Progression(status, progression01, progression02, score);
    }

    public void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId)
    {
        if (!IsActive || amount <= 0f || string.IsNullOrEmpty(currency)) return;
        _backend.Resource(flow, currency, amount, itemType, itemId);
    }

    public void Design(string eventId, float? value = null)
    {
        if (!IsActive || string.IsNullOrEmpty(eventId)) return;
        _backend.Design(eventId, value);
    }
}
