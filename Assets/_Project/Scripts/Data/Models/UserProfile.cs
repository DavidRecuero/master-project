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

    public UserProfile()
    {
        CurrentLevel = 1;       //starting values
        Coins = 250;

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