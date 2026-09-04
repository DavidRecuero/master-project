using UnityEngine;

public class LocalJsonUserDataService : IUserDataService
{
    private const string SaveKey = "UserProfileData";
    private readonly IStorageProvider _storage;

    public LocalJsonUserDataService(IStorageProvider storage = null)
    {
        _storage = storage ?? new PlayerPrefsStorageProvider();
    }

    public void SaveProfile(UserProfile profile)
    {
        string json = JsonUtility.ToJson(profile);
        _storage.SetString(SaveKey, json);
        _storage.Save();
    }

    public UserProfile LoadProfile()
    {
        if (!HasProfile())
        {
            UserProfile newProfile = new UserProfile();
            SaveProfile(newProfile);
            return newProfile;
        }

        string json = _storage.GetString(SaveKey);
        return JsonUtility.FromJson<UserProfile>(json);
    }

    public bool HasProfile()
    {
        return _storage.HasKey(SaveKey);
    }
}