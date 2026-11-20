using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The level catalog shared by the whole game. The Boot loads it from Addressables without blocking (LoadAsync);
/// from then on LevelLoader reads it synchronously.
///
/// Without the Boot (Play on the Level scene, editor tools, EditMode tests) the first read loads it then:
/// in Play mode with Addressables (blocking, fine for local content), outside Play mode straight from the
/// Levels folder with the AssetDatabase (Addressables is a runtime system).
/// </summary>
public static class LevelLibrary
{
    /// <summary>Addressables label of every level file.</summary>
    public const string Label = "level";

    /// <summary>Folder of the level files in the project (the "Levels" Addressables group).</summary>
    public const string Folder = "Assets/_Project/Levels";

    private static ILevelSource _source;
    private static LevelCatalog _catalog;

    public static bool IsLoaded => _catalog != null;

    private static ILevelSource Source => _source ??= new AddressablesLevelSource();

    /// <summary>Loads the catalog (Boot). Never throws: on failure it stays unloaded and the first read retries.</summary>
    public static async Task<LevelCatalog> LoadAsync()
    {
        try
        {
            _catalog = await Source.LoadAsync() ?? LevelCatalog.Empty;
            Log(_catalog);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Levels] Couldn't load the levels now, retrying when the first level is read: {e.Message}");
        }
        return _catalog;
    }

    /// <summary>The catalog, loading it now if nobody did.</summary>
    public static LevelCatalog EnsureLoaded()
    {
        if (_catalog != null) return _catalog;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _catalog = LoadFromFolder();
            return _catalog;
        }
#endif

        try
        {
            _catalog = Source.LoadNow() ?? LevelCatalog.Empty;
            Log(_catalog);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Levels] No levels: {e.Message}");
            _catalog = LevelCatalog.Empty;
        }
        return _catalog;
    }

    /// <summary>Replaces where the levels come from (tests). Forgets the loaded catalog.</summary>
    public static void SetSource(ILevelSource source)
    {
        _source = source;
        _catalog = null;
    }

    /// <summary>Forgets the catalog and goes back to Addressables. Also on entering Play mode without domain reload.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => SetSource(null);

    private static void Log(LevelCatalog catalog)
    {
        Debug.Log($"[Levels] {catalog.Count} levels ready.");
        if (catalog.Numbers.Count > catalog.Count)
            Debug.LogWarning($"[Levels] Level {catalog.Count + 1} is missing: the {catalog.Numbers.Count - catalog.Count} levels after it can't be played.");
    }

#if UNITY_EDITOR
    private static LevelCatalog LoadFromFolder()
    {
        var levels = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();
        if (!UnityEditor.AssetDatabase.IsValidFolder(Folder)) return LevelCatalog.Empty;

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:TextAsset", new[] { Folder }))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null) levels.Add(new System.Collections.Generic.KeyValuePair<string, string>(path, asset.text));
        }
        return new LevelCatalog(levels);
    }
#endif
}
