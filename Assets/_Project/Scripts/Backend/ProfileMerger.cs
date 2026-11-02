/// <summary>Which copy of the profile wins when the device and the cloud don't match.</summary>
public enum ProfileSource
{
    Local,
    Cloud
}

/// <summary>
/// Picks between the profile on the device and the one in the cloud. Pure logic, no SDK.
///
/// Rule (chosen so the player never loses progress):
///   1. No cloud copy: the device wins (first sync, or new account).
///   2. The one with the higher level wins.
///   3. Same level: the one saved last wins (UpdatedAtTicks, UTC).
///   4. Full tie: the device wins (nothing to download).
///
/// Known trade-off: two different accounts on the same device share the device copy, so the
/// first sync of the second account can take the first one's progress. Accepted: rare, and
/// losing progress would be worse.
/// </summary>
public static class ProfileMerger
{
    public static ProfileSource Choose(UserProfile local, UserProfile cloud)
    {
        if (cloud == null) return ProfileSource.Local;
        if (local == null) return ProfileSource.Cloud;

        if (cloud.CurrentLevel != local.CurrentLevel)
            return cloud.CurrentLevel > local.CurrentLevel ? ProfileSource.Cloud : ProfileSource.Local;

        return cloud.UpdatedAtTicks > local.UpdatedAtTicks ? ProfileSource.Cloud : ProfileSource.Local;
    }
}
