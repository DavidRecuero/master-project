using UnityEngine;

/// <summary>
/// Reads levels from the shared catalog (LevelLibrary), which the Boot loads from Addressables.
/// Synchronous on purpose: every level is already in memory, so the gameplay never waits for content.
/// </summary>
public class LevelLoader : ILevelLoader
{
    // Loads and returns the data from the JSON's level
    public LevelData LoadLevel(int levelIndex)
    {
        if (!LevelLibrary.EnsureLoaded().TryGetJson(levelIndex, out string json))
        {
            Debug.LogError($"Cannot find {FileName(levelIndex)} in the '{LevelLibrary.Label}' Addressables label!");
            return null;
        }

        // Parse the JSON text into our C# class
        return JsonUtility.FromJson<LevelData>(json);
    }

    // Levels playable in order: level_001, level_002... until the first missing number.
    // A gap in the numbering hides every level after it: LevelLoaderTests checks there are none.
    public int CountLevels() => LevelLibrary.EnsureLoaded().Count;

    // Formats 1 into "level_001", 10 into "level_010", etc.
    public static string FileName(int levelIndex) => LevelCatalog.Address(levelIndex);
}
