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

        // Starter boosters so new players can try them out for free
        Boosters.Add(new BoosterEntry { type = BoosterType.Match, count = 15 });
        Boosters.Add(new BoosterEntry { type = BoosterType.Shuffle, count = 15 });
        Boosters.Add(new BoosterEntry { type = BoosterType.TrayClearer, count = 15 });
    }

    public int GetBoosterCount(BoosterType type)
    {
        BoosterEntry entry = Boosters.Find(b => b.type == type);
        return entry?.count ?? 0;
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