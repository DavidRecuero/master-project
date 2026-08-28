[System.Serializable]
public class UserProfile
{
    public string UserId;
    public int CurrentLevel;
    public int Coins;
    public string Segment;

    public UserProfile()
    {
        CurrentLevel = 1;
        Coins = 0;
    }
}