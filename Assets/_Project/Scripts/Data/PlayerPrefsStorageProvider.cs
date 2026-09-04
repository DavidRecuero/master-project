using UnityEngine;

public class PlayerPrefsStorageProvider : IStorageProvider
{
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public void Save() => PlayerPrefs.Save();
}