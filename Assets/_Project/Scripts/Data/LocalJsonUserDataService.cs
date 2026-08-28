using UnityEngine;

public class LocalJsonUserDataService : IUserDataService
{
    private const string SaveKey = "UserProfileData";

    public void SaveProfile(UserProfile profile)
    {
        string json = JsonUtility.ToJson(profile);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public UserProfile LoadProfile()
    {
        if (!HasProfile())
        {
            UserProfile newProfile = new UserProfile();
            SaveProfile(newProfile);
            return newProfile;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        return JsonUtility.FromJson<UserProfile>(json);
    }

    public bool HasProfile()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }
}