public interface IStorageProvider
{
    void SetString(string key, string value);
    string GetString(string key, string defaultValue = "");
    bool HasKey(string key);
    void Save();
}