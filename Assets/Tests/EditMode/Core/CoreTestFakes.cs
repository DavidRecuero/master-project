using System.Collections.Generic;

public class FakeUserDataProvider : IUserDataProvider
{
    public int CurrentLevel { get; set; } = 1;
    public int Coins { get; set; } = 100;
    public bool ResetDataCalled { get; private set; }

    public void ResetData()
    {
        ResetDataCalled = true;
        CurrentLevel = 1;
        Coins = 0;
    }
}

public class FakeSceneLoader : ISceneLoader
{
    public int LoadedSceneIndex { get; private set; } = -1;
    public bool ReloadCalled { get; private set; }

    public void LoadScene(int sceneIndex) => LoadedSceneIndex = sceneIndex;
    public void ReloadCurrentScene() => ReloadCalled = true;
}

public class FakeStorageProvider : IStorageProvider
{
    private readonly Dictionary<string, string> _store = new();

    public void SetString(string key, string value) => _store[key] = value;
    public string GetString(string key, string defaultValue = "") =>
        _store.TryGetValue(key, out var val) ? val : defaultValue;
    public bool HasKey(string key) => _store.ContainsKey(key);
    public void Save() { }
}