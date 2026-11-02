using System;

/// <summary>
/// The player profile kept on the device (UserDataManager), seen from the cloud sync.
/// </summary>
public interface IProfileRepository
{
    UserProfile Profile { get; }

    /// <summary>Raised after every save on the device (level cleared, booster used, coins spent...).</summary>
    event Action ProfileSaved;

    /// <summary>Takes the cloud copy as it is (keeps its timestamp) and saves it on the device.</summary>
    void ReplaceProfile(UserProfile profile);

    /// <summary>Marks the profile as belonging to this backend player and saves it.</summary>
    void SetOwner(string playerId);
}
