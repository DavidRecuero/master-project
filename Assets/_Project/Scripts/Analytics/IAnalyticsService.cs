/// <summary>
/// What the game uses to send analytics. Everything goes through here, never to the provider directly: this is
/// where consent is checked. Without consent every call does nothing, so the game can call it anywhere without
/// asking first.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>True when events are being sent: the provider is started and the player said yes.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Starts following the player's consent: starts the provider now if there is a yes, and from then on starts
    /// or stops sending when the player changes their mind (GameEvents.OnConsentChanged). Called once, by the Boot.
    /// </summary>
    void Start();

    /// <summary>Stops following the consent (tests, shutdown). The sending is left as it is.</summary>
    void Stop();

    void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null);
    void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId);
    void Design(string eventId, float? value = null);
}
