using UnityEngine;

public class LevelLoader : ILevelLoader
{
    // Loads and returns the data from the JSON's level
    public LevelData LoadLevel(int levelIndex)
    {
        // Formats 1 into "level_001", 10 into "level_010", etc.
        string fileName = $"level_{levelIndex:D3}";

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
}