using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lists every level_XXX.json in Resources and lets you play any of them directly.
/// </summary>
public class LevelLoaderWindow : EditorWindow
{
    private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";

    private const string StartFromBootPrefKey = "MastersProject.LevelLoader.StartFromBoot";
    private const string KeepOverridePrefKey = "MastersProject.LevelLoader.KeepOverride";

    private static readonly Regex LevelNameRegex = new Regex(@"^level_(\d+)$");

    private class LevelEntry
    {
        public int number;
        public string assetPath;
        public string summary;
    }

    private List<LevelEntry> levels = new List<LevelEntry>();
    private Vector2 scroll;

    [MenuItem("Tools/Level Loader")]
    public static void Open()
    {
        LevelLoaderWindow window = GetWindow<LevelLoaderWindow>("Level Loader");
        window.minSize = new Vector2(320, 300);
        window.RefreshLevels();
    }

    private void OnEnable() => RefreshLevels();

    private void OnFocus() => RefreshLevels();

    // ---------- Data ----------

    private void RefreshLevels()
    {
        levels.Clear();

        foreach (string guid in AssetDatabase.FindAssets("level_ t:TextAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("/Resources/")) continue; // LevelLoader only reads from Resources

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null) continue;

            Match match = LevelNameRegex.Match(asset.name);
            if (!match.Success) continue;

            levels.Add(new LevelEntry
            {
                number = int.Parse(match.Groups[1].Value),
                assetPath = path,
                summary = BuildSummary(asset.text)
            });
        }

        levels = levels.OrderBy(l => l.number).ToList();
        Repaint();
    }

    // One line describing the level: board size, pipes, items and obstacles
    private static string BuildSummary(string json)
    {
        LevelData data;
        try { data = JsonUtility.FromJson<LevelData>(json); }
        catch { return "⚠ Invalid JSON"; }

        if (data == null || data.pipes == null) return "⚠ Invalid level data";

        List<ItemSpawnData> allItems = data.pipes.SelectMany(p => p.BuildSpawnQueue()).ToList();
        int hidden = allItems.Count(i => i.type == ItemType.Hidden);
        int barrels = allItems.Count(i => i.type == ItemType.Barrel);

        string summary = $"{data.width}x{data.height} · {data.pipes.Count} pipes · {allItems.Count} items";
        if (hidden > 0) summary += $" · {hidden} hidden";
        if (barrels > 0) summary += $" · {barrels} barrels";
        return summary;
    }

    // ---------- UI ----------

    private void OnGUI()
    {
        DrawStatus();
        EditorGUILayout.Space();
        DrawOptions();
        EditorGUILayout.Space();
        DrawLevelList();
    }

    private void DrawStatus()
    {
        int current = DebugLevelOverride.Level;

        if (current > 0)
        {
            EditorGUILayout.HelpBox($"Override active: level {current} will be loaded instead of the saved one.", MessageType.Info);
            if (GUILayout.Button("Clear override")) ClearOverride();
        }
        else
        {
            EditorGUILayout.HelpBox("No override: the game loads the player's saved level.", MessageType.None);
        }
    }

    private void DrawOptions()
    {
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

        bool startFromBoot = EditorPrefs.GetBool(StartFromBootPrefKey, true);
        bool newStartFromBoot = EditorGUILayout.ToggleLeft(
            new GUIContent("Start from Boot scene", "Recommended: initializes user data and boosters. You'll need to press Play in the main menu."),
            startFromBoot);
        if (newStartFromBoot != startFromBoot) EditorPrefs.SetBool(StartFromBootPrefKey, newStartFromBoot);

        bool keepOverride = EditorPrefs.GetBool(KeepOverridePrefKey, false);
        bool newKeepOverride = EditorGUILayout.ToggleLeft(
            new GUIContent("Keep override after exiting Play mode", "If off, the override is cleared automatically when you stop playing."),
            keepOverride);
        if (newKeepOverride != keepOverride) EditorPrefs.SetBool(KeepOverridePrefKey, newKeepOverride);
    }

    private void DrawLevelList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Levels ({levels.Count})", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) RefreshLevels();
        EditorGUILayout.EndHorizontal();

        if (levels.Count == 0)
        {
            EditorGUILayout.HelpBox("No level_XXX.json found inside a Resources folder.", MessageType.Warning);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (LevelEntry level in levels)
        {
            bool isCurrent = DebugLevelOverride.Level == level.number;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(isCurrent ? $"▶ Level {level.number}" : $"Level {level.number}",
                            EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label(level.summary, EditorStyles.miniLabel);

            if (GUILayout.Button("Ping", GUILayout.Width(40)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(level.assetPath));
            }

            if (GUILayout.Button("▶ Play", GUILayout.Width(60)))
            {
                PlayLevel(level.number);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // ---------- Actions ----------

    private void PlayLevel(int levelNumber)
    {
        EditorPrefs.SetInt(DebugLevelOverride.LevelPrefKey, levelNumber);

        if (EditorApplication.isPlaying)
        {
            // Already playing: if we're in the level scene, reload it so the GameManager reads the new override
            if (SceneManager.GetActiveScene().path == LevelScenePath)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                Debug.Log($"🔧 [Level Loader] Level {levelNumber} will load the next time the Level scene opens.");
            }
            return;
        }

        // Not playing: open the start scene and enter Play mode
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        bool startFromBoot = EditorPrefs.GetBool(StartFromBootPrefKey, true);
        string scenePath = startFromBoot ? BootScenePath : LevelScenePath;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            Debug.LogError($"❌ [Level Loader] Scene not found at {scenePath}. Update the path in LevelLoaderWindow.cs.");
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.EnterPlaymode();
    }

    private static void ClearOverride()
    {
        EditorPrefs.DeleteKey(DebugLevelOverride.LevelPrefKey);
    }

    // Clears the override when leaving Play mode, unless the user chose to keep it
    [InitializeOnLoadMethod]
    private static void RegisterPlayModeCallback()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (EditorPrefs.GetBool(KeepOverridePrefKey, false)) return;

        ClearOverride();
    }
}
