public interface ILevelLoader
{
    LevelData LoadLevel(int levelIndex);

    /// <summary>
    /// Number of designed levels: level_001, level_002... counted until the first missing number.
    /// </summary>
    int CountLevels();
}
