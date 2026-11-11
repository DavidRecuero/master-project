/// <summary>Where the player is in a progression (a level).</summary>
public enum AnalyticsProgressionStatus
{
    Start,
    Complete,
    Fail,
}

/// <summary>Direction of a resource: Source = the player gets it, Sink = the player spends it.</summary>
public enum AnalyticsResourceFlow
{
    Source,
    Sink,
}

/// <summary>
/// The analytics provider behind the game (GameAnalytics today). Only AnalyticsService talks to it, and only
/// with consent: the game never calls the provider directly, so changing provider means writing one new backend.
/// </summary>
public interface IAnalyticsBackend
{
    /// <summary>True once the provider was started. It is never "un-started": withdrawing consent stops the sending.</summary>
    bool IsInitialized { get; }

    /// <summary>Starts the provider (session, automatic error reports...). Called once, the first time there is consent.</summary>
    void Initialize();

    /// <summary>Turns the sending on or off. Off also drops what would be sent: nothing is kept for later.</summary>
    void SetSubmissionEnabled(bool enabled);

    /// <summary>Progression event, e.g. Start "Level_012". score: optional (only Complete/Fail).</summary>
    void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null);

    /// <summary>Resource event, e.g. Source "Coins" 20 "Reward" "LevelWin". Currency and item type must be declared in the provider settings.</summary>
    void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId);

    /// <summary>Design (custom) event, e.g. "Booster:Used:Shuffle". Up to 5 parts separated by ':'.</summary>
    void Design(string eventId, float? value = null);
}
