using UnityEngine;

public class LevelLoader : ILevelLoader
{
    // Levels are counted once per session (they ship inside the build and can't change while it runs)
    private static int? _cachedLevelCount;

    // Loads and returns the data from the JSON's level
    public LevelData LoadLevel(int levelIndex)
    {
        string fileName = FileName(levelIndex);

        // Load the JSON file from the Resources folder
        TextAsset jsonTextFile = Resources.Load<TextAsset>(fileName);

        if (jsonTextFile == null)
        {
            Debug.LogError($"Cannot find {fileName}.json in the Resources folder!");
            return null;
        }

        // Parse the JSON text into our C# class 
        return JsonUtility.FromJson<LevelData>(jsonTextFile.text);
    }

    // Counts level_001, level_002... until the first missing number.
    // A gap in the numbering hides every level after it: LevelLoaderTests checks there are none.
    public int CountLevels()
    {
        if (_cachedLevelCount.HasValue) return _cachedLevelCount.Value;

        int count = 0;
        while (true)
        {
            TextAsset level = Resources.Load<TextAsset>(FileName(count + 1));
            if (level == null) break;

            Resources.UnloadAsset(level);
            count++;
        }

        _cachedLevelCount = count;
        return count;
    }

    // Formats 1 into "level_001", 10 into "level_010", etc.
    public static string FileName(int levelIndex) => $"level_{levelIndex:D3}";

    // Clears the cache when entering Play mode, also with Domain Reload disabled
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        _cachedLevelCount = null;
    }
}
