using System.Collections.Generic;

[System.Serializable]
public class BoosterEntry
{
    public BoosterType type;
    public int count;
}

[System.Serializable]
public class UserProfile
{
    public string UserId;
    public int CurrentLevel;
    public int Coins;
    public string Segment;
    public List<BoosterEntry> Boosters = new List<BoosterEntry>();

    /// <summary>
    /// Last save on the device, in UTC ticks. Breaks ties between the device and the cloud copy
    /// (see ProfileMerger). 0 in profiles saved before the cloud sync existed.
    /// </summary>
    public long UpdatedAtTicks;

    /// <summary>
    /// Lives the player is missing (0 = full). Stored as missing, not as lives, so profiles saved before the
    /// lives system existed load full. Read the lives through ILivesService, never from here.
    /// </summary>
    public int MissingLives;

    /// <summary>When the countdown of the next life started, in UTC ticks. 0 when full. See LivesService.</summary>
    public long NextLifeStartTicks;

    /// <summary>
    /// When the player started playing, in UTC ticks (segmentation: days since install). A profile saved before this
    /// field existed gets the time it was first loaded with this version (the constructor value is kept).
    /// </summary>
    public long CreatedAtTicks;

    /// <summary>Levels lost or abandoned in a row since the last win (segmentation: players who are stuck).</summary>
    public int FailStreak;

    /// <summary>Last launch, in UTC ticks (segmentation: players coming back). 0 before the first one.</summary>
    public long LastSessionTicks;

    /// <summary>Different UTC days with at least one launch (segmentation: active players).</summary>
    public int DaysPlayed;

    public UserProfile()
    {
        CurrentLevel = 1;       //starting values
        Coins = 250;
        CreatedAtTicks = System.DateTime.UtcNow.Ticks;

        // Booster starter stock is no longer hardcoded here - BoosterManager grants it
        // from BoosterDefinition assets the first time it initializes (see EnsureStarterStock).
    }

    public int GetBoosterCount(BoosterType type)
    {
        BoosterEntry entry = Boosters.Find(b => b.type == type);
        return entry?.count ?? 0;
    }

    public bool HasBoosterEntry(BoosterType type)
    {
        return Boosters.Exists(b => b.type == type);
    }

    public void SetBoosterCount(BoosterType type, int newCount)
    {
        BoosterEntry entry = Boosters.Find(b => b.type == type);
        if (entry != null)
        {
            entry.count = newCount;
        }
        else
        {
            Boosters.Add(new BoosterEntry { type = type, count = newCount });
        }
    }
}