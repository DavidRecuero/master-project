/// <summary>
/// Editor bridge between the "Tools/Level Loader" window and the GameManager
/// </summary>
public static class DebugLevelOverride
{
    public const string LevelPrefKey = "MastersProject.DebugLevelOverride";

    // Level to force, or 0 when no override is active.
    public static int Level
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetInt(LevelPrefKey, 0);
#else
            return 0;
#endif
        }
    }

    public static bool IsActive => Level > 0;
}
