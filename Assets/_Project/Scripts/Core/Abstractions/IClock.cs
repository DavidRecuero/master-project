using System;

/// <summary>
/// The current time, behind an interface so the code that depends on it (lives regeneration) can be tested
/// with a fake clock that moves on demand.
/// </summary>
public interface IClock
{
    /// <summary>Current time in UTC. Always UTC: the device time zone can change between sessions.</summary>
    DateTime UtcNow { get; }
}
