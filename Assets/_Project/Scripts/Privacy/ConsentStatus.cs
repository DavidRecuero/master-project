/// <summary>The player's answer for one purpose.</summary>
public enum ConsentStatus
{
    /// <summary>Not asked yet, or asked for an older consent version. Counts as no.</summary>
    Unknown = 0,
    Granted = 1,
    Denied = 2,
}
