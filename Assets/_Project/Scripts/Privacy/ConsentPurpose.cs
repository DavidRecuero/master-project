/// <summary>
/// What the player can say yes or no to. Only the uses that need consent are here: the cloud save and the
/// sign-in are needed for the game to work, so they don't ask.
/// The number is saved on the device: add new purposes at the end and never reuse a number.
/// </summary>
public enum ConsentPurpose
{
    /// <summary>Analytics and crash reports sent to a third party (GameAnalytics).</summary>
    Analytics = 0,
}
