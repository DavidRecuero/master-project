using System;

/// <summary>
/// The only place the game asks "may I do this with the player's data?". Every service that needs consent
/// (analytics now, ads later) asks here before starting, and listens to GameEvents.OnConsentChanged to start or
/// stop when the player changes their mind. When a certified CMP comes in for ads (Google UMP), it goes behind
/// this interface too, so the rest of the game doesn't change.
/// </summary>
public interface IConsentService
{
    /// <summary>The consent popup must be shown: some purpose was never answered, or was answered for an older version.</summary>
    bool NeedsPrompt { get; }

    /// <summary>The answer for the current consent version. An answer for an older version counts as Unknown.</summary>
    ConsentStatus GetStatus(ConsentPurpose purpose);

    /// <summary>True only with a yes for the current version. Unknown counts as no.</summary>
    bool IsGranted(ConsentPurpose purpose);

    /// <summary>When the player last decided (UTC), or null if never.</summary>
    DateTime? DecisionUtc { get; }

    /// <summary>Consent popup: "Accept" (true) or "Reject" (false) for every purpose at once.</summary>
    void SetAll(bool granted);

    /// <summary>Privacy preferences in Settings: changes one purpose.</summary>
    void Set(ConsentPurpose purpose, bool granted);

    /// <summary>Debug: forgets every answer, so the popup shows again. Everything that needed consent stops.</summary>
    void Reset();
}
