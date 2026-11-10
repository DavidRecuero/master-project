using System;

/// <summary>
/// The player's privacy choices (GDPR opt-in, applied to every player wherever they are: the strictest model also
/// covers the CCPA). Pure C#: the storage and the clock come from outside.
///
/// Consent version: CurrentVersion goes up when what we ask for changes (a new purpose, a new third party that
/// gets data). Answers given for an older version count as Unknown, so the popup shows again and nothing that needs
/// consent starts until the player answers. A small change in the policy text (typos, contact) doesn't need it.
///
/// Every real change of a purpose (from "may use" to "may not", or back) raises GameEvents.OnConsentChanged.
/// </summary>
public sealed class ConsentService : IConsentService
{
    /// <summary>Raise it when the consent popup asks for something new. 1 = analytics (GameAnalytics), 0.5.0.</summary>
    public const int CurrentVersion = 1;

    private static readonly ConsentPurpose[] AllPurposes = (ConsentPurpose[])Enum.GetValues(typeof(ConsentPurpose));

    private readonly ConsentStore _store;
    private readonly IClock _clock;
    private readonly int _version;

    /// <param name="version">Only for tests: the consent version to ask for. CurrentVersion by default.</param>
    public ConsentService(ConsentStore store, IClock clock, int version = CurrentVersion)
    {
        _store = store;
        _clock = clock ?? new SystemClock();
        _version = Math.Max(1, version);
    }

    public bool NeedsPrompt
    {
        get
        {
            foreach (ConsentPurpose purpose in AllPurposes)
                if (GetStatus(purpose) == ConsentStatus.Unknown) return true;
            return false;
        }
    }

    public ConsentStatus GetStatus(ConsentPurpose purpose)
    {
        if (_store == null || _store.Version < _version) return ConsentStatus.Unknown;
        return _store.GetStatus(purpose);
    }

    public bool IsGranted(ConsentPurpose purpose) => GetStatus(purpose) == ConsentStatus.Granted;

    public DateTime? DecisionUtc => _store?.DecisionUtc;

    public void SetAll(bool granted)
    {
        foreach (ConsentPurpose purpose in AllPurposes)
            Set(purpose, granted);
    }

    public void Set(ConsentPurpose purpose, bool granted)
    {
        if (_store == null) return;

        bool wasGranted = IsGranted(purpose);
        _store.Save(purpose, granted ? ConsentStatus.Granted : ConsentStatus.Denied, _version, _clock.UtcNow);

        if (wasGranted != granted) GameEvents.TriggerConsentChanged(purpose, granted);
    }

    public void Reset()
    {
        if (_store == null) return;

        // Read before clearing: only the purposes that were in use have something to stop
        var wasGranted = new bool[AllPurposes.Length];
        for (int i = 0; i < AllPurposes.Length; i++) wasGranted[i] = IsGranted(AllPurposes[i]);

        _store.Clear();

        for (int i = 0; i < AllPurposes.Length; i++)
            if (wasGranted[i]) GameEvents.TriggerConsentChanged(AllPurposes[i], false);
    }
}
