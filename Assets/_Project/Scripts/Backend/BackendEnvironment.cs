using UnityEngine;

/// <summary>
/// UGS environment for this build: the Editor and development builds use "development",
/// release builds (the AABs uploaded to Google Play) use "production".
/// So tests in the Editor never touch the testers' and players' data.
/// </summary>
public static class BackendEnvironment
{
    public const string Development = "development";
    public const string Production = "production";

    /// <summary>Debug.isDebugBuild is true in the Editor and in builds with "Development Build" checked.</summary>
    public static string Current => Resolve(Application.isEditor, Debug.isDebugBuild);

    public static string Resolve(bool isEditor, bool isDevelopmentBuild)
    {
        return isEditor || isDevelopmentBuild ? Development : Production;
    }
}
