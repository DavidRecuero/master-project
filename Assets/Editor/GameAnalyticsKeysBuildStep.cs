using System;
using System.IO;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Setup;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Keeps the GameAnalytics keys out of the (public) repo. Resources/GameAnalytics/Settings.asset is versioned with
/// all its options but with empty keys; the keys are written into it only while an Android build runs, and emptied
/// again when it ends.
///
/// Where the keys come from, in this order:
///   1. Environment variables GA_ANDROID_GAME_KEY and GA_ANDROID_SECRET_KEY (CI, e.g. UGS Build Automation).
///   2. UserSettings/GameAnalyticsKeys.json on this computer (UserSettings/ is in .gitignore).
///      Tools > GameAnalytics > Open Local Keys File creates it.
/// Without keys the build still works, but the game sends no analytics (a warning says so).
///
/// If a build fails half-way, the keys could stay in the asset: the next editor reload empties them (and git would
/// show Settings.asset as changed). Never commit Settings.asset with keys in it.
/// </summary>
public class GameAnalyticsKeysBuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public const string GameKeyVariable = "GA_ANDROID_GAME_KEY";
    public const string SecretKeyVariable = "GA_ANDROID_SECRET_KEY";
    public const string LocalKeysPath = "UserSettings/GameAnalyticsKeys.json";
    private const string InjectedFlag = "PipeMess.GameAnalyticsKeysInjected";

    [Serializable]
    private class LocalKeys
    {
        public string androidGameKey = "";
        public string androidSecretKey = "";
    }

    // Before the other build steps, so the keys are in the settings when they are packed
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;

        Settings settings = GameAnalytics.SettingsGA;
        int index = AndroidIndex(settings);
        if (index < 0)
        {
            Debug.LogWarning("[GameAnalytics] Android is not set up in Resources/GameAnalytics/Settings (Setup > Add platform). This build sends no analytics.");
            return;
        }

        if (!TryGetKeys(out string gameKey, out string secretKey, out string source))
        {
            Debug.LogWarning($"[GameAnalytics] No keys found ({GameKeyVariable}/{SecretKeyVariable} or {LocalKeysPath}). This build sends no analytics.");
            return;
        }

        WriteKeys(settings, index, gameKey, secretKey);
        SessionState.SetBool(InjectedFlag, true);
        Debug.Log($"[GameAnalytics] Keys added to the build (from {source}).");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        ClearKeys();
    }

    /// <summary>Empties the keys if a build left them in the asset (failed build). Runs on every editor reload.</summary>
    [InitializeOnLoadMethod]
    private static void ClearLeftovers()
    {
        if (!SessionState.GetBool(InjectedFlag, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (!BuildPipeline.isBuildingPlayer) ClearKeys();
        };
    }

    [MenuItem("Tools/GameAnalytics/Open Local Keys File")]
    private static void OpenLocalKeysFile()
    {
        if (!File.Exists(LocalKeysPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LocalKeysPath));
            File.WriteAllText(LocalKeysPath, JsonUtility.ToJson(new LocalKeys(), true));
            Debug.Log($"[GameAnalytics] Created {LocalKeysPath}. Paste the Android game key and secret key there.");
        }

        EditorUtility.RevealInFinder(Path.GetFullPath(LocalKeysPath));
    }

    [MenuItem("Tools/GameAnalytics/Check Keys")]
    private static void CheckKeys()
    {
        Settings settings = GameAnalytics.SettingsGA;
        int index = AndroidIndex(settings);
        bool inAsset = index >= 0 && (!string.IsNullOrEmpty(settings.GetGameKey(index)) || !string.IsNullOrEmpty(settings.GetSecretKey(index)));
        bool found = TryGetKeys(out _, out _, out string source);

        Debug.Log($"[GameAnalytics] Android in settings: {(index >= 0 ? "yes" : "NO")} | " +
                  $"keys for the build: {(found ? "found in " + source : "NOT FOUND")} | " +
                  $"keys inside Settings.asset: {(inAsset ? "YES (empty them before committing)" : "no (good)")}");
    }

    private static int AndroidIndex(Settings settings) =>
        settings == null ? -1 : settings.Platforms.IndexOf(RuntimePlatform.Android);

    private static bool TryGetKeys(out string gameKey, out string secretKey, out string source)
    {
        gameKey = Environment.GetEnvironmentVariable(GameKeyVariable);
        secretKey = Environment.GetEnvironmentVariable(SecretKeyVariable);
        source = "environment variables";
        if (IsSet(gameKey) && IsSet(secretKey)) return true;

        source = LocalKeysPath;
        gameKey = secretKey = null;
        if (!File.Exists(LocalKeysPath)) return false;

        try
        {
            LocalKeys keys = JsonUtility.FromJson<LocalKeys>(File.ReadAllText(LocalKeysPath));
            gameKey = keys?.androidGameKey?.Trim();
            secretKey = keys?.androidSecretKey?.Trim();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameAnalytics] Could not read {LocalKeysPath}: {e.Message}");
            return false;
        }

        return IsSet(gameKey) && IsSet(secretKey);
    }

    private static bool IsSet(string value) => !string.IsNullOrWhiteSpace(value);

    private static void ClearKeys()
    {
        SessionState.EraseBool(InjectedFlag);
        Settings settings = GameAnalytics.SettingsGA;
        int index = AndroidIndex(settings);
        if (index < 0) return;
        if (string.IsNullOrEmpty(settings.GetGameKey(index)) && string.IsNullOrEmpty(settings.GetSecretKey(index))) return;

        WriteKeys(settings, index, string.Empty, string.Empty);
        Debug.Log("[GameAnalytics] Keys removed from Settings.asset after the build.");
    }

    private static void WriteKeys(Settings settings, int index, string gameKey, string secretKey)
    {
        Settings.UpdateKeys(index, gameKey, secretKey);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
    }
}
