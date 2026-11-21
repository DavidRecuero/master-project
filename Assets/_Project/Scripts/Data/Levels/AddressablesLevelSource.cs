using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// Levels from Addressables: every TextAsset with the "level" label (group "Levels", addresses "level_001"...).
/// The JSON text is copied and the assets released right away: a level is a few KB, so the whole catalog stays
/// in memory and the gameplay reads it synchronously, with no loading screens between levels.
///
/// A level that can't be loaded is left out with a warning instead of failing the whole catalog: a remote level
/// (CCD) that was never downloaded can't load offline, and the game must still play every level it has.
/// </summary>
public class AddressablesLevelSource : ILevelSource
{
    private readonly string _label;

    public AddressablesLevelSource(string label = LevelLibrary.Label)
    {
        _label = label;
    }

    public async Task<LevelCatalog> LoadAsync()
    {
        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
            Addressables.LoadResourceLocationsAsync(_label, typeof(TextAsset));
        try
        {
            IList<IResourceLocation> locations = await locationsHandle.Task;
            ThrowIfFailed(locationsHandle, $"locations of the '{_label}' label");

            // All at once: with remote content (CCD) the downloads overlap
            var loads = Distinct(locations).Select(LoadTextAsync).ToList();
            KeyValuePair<string, string>[] levels = await Task.WhenAll(loads);
            return new LevelCatalog(levels);
        }
        finally
        {
            if (locationsHandle.IsValid()) Addressables.Release(locationsHandle);
        }
    }

    public LevelCatalog LoadNow()
    {
        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
            Addressables.LoadResourceLocationsAsync(_label, typeof(TextAsset));
        try
        {
            IList<IResourceLocation> locations = locationsHandle.WaitForCompletion();
            ThrowIfFailed(locationsHandle, $"locations of the '{_label}' label");

            var levels = new List<KeyValuePair<string, string>>();
            foreach (IResourceLocation location in Distinct(locations))
            {
                AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(location);
                try
                {
                    TextAsset asset = handle.WaitForCompletion();
                    ThrowIfFailed(handle, location.PrimaryKey);
                    levels.Add(new KeyValuePair<string, string>(location.PrimaryKey, asset != null ? asset.text : null));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Levels] {location.PrimaryKey} not available now: {e.Message}");
                }
                finally
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                }
            }
            return new LevelCatalog(levels);
        }
        finally
        {
            if (locationsHandle.IsValid()) Addressables.Release(locationsHandle);
        }
    }

    private static async Task<KeyValuePair<string, string>> LoadTextAsync(IResourceLocation location)
    {
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(location);
        try
        {
            TextAsset asset = await handle.Task;
            ThrowIfFailed(handle, location.PrimaryKey);
            return new KeyValuePair<string, string>(location.PrimaryKey, asset != null ? asset.text : null);
        }
        catch (Exception e)
        {
            // Left out (null JSON): the catalog ignores it and the levels after it wait for the next launch
            Debug.LogWarning($"[Levels] {location.PrimaryKey} not available now: {e.Message}");
            return new KeyValuePair<string, string>(location.PrimaryKey, null);
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    // The same asset can come with more than one location (e.g. one per key): one load each
    private static IEnumerable<IResourceLocation> Distinct(IList<IResourceLocation> locations) =>
        (locations ?? new List<IResourceLocation>())
        .GroupBy(location => location.PrimaryKey)
        .Select(group => group.First());

    private static void ThrowIfFailed(AsyncOperationHandle handle, string what)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
            throw new InvalidOperationException($"Addressables couldn't load the {what}: {handle.OperationException?.Message}");
    }
}
