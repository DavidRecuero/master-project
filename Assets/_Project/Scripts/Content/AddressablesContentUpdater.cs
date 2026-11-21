using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// IContentUpdater on top of Addressables. The automatic catalog update at startup is off in the Addressables
/// settings, so it happens here, in the Boot, where it has a time limit and a log:
///   1. Ask CCD if there is a newer catalog (new content published) and load it.
///   2. Download the level bundles that aren't on the device yet (Addressables keeps them in its cache, so they
///      also work offline afterwards).
/// </summary>
public class AddressablesContentUpdater : IContentUpdater
{
    private readonly string _label;

    public AddressablesContentUpdater(string label = LevelLibrary.Label)
    {
        _label = label;
    }

    public async Task<ContentUpdateResult> UpdateAsync()
    {
        // No bucket yet for this environment: nothing to ask (and no failed request to a URL that can't exist)
        string environment = BackendEnvironment.Current;
        if (!CcdContent.HasBucket(environment))
            throw new InvalidOperationException($"no CCD bucket set for '{environment}' (CcdContent)");

        AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> init = Addressables.InitializeAsync(false);
        try
        {
            await init.Task;
            ThrowIfFailed(init, "initialize");
        }
        finally
        {
            Release(init);
        }

        int catalogsUpdated = await UpdateCatalogsAsync();
        long bytes = await DownloadAsync();
        return new ContentUpdateResult(catalogsUpdated, bytes);
    }

    private static async Task<int> UpdateCatalogsAsync()
    {
        List<string> catalogs;
        AsyncOperationHandle<List<string>> check = Addressables.CheckForCatalogUpdates(false);
        try
        {
            catalogs = await check.Task;
            ThrowIfFailed(check, "check the remote catalog");
        }
        finally
        {
            Release(check);
        }

        if (catalogs == null || catalogs.Count == 0) return 0;

        AsyncOperationHandle<List<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator>> update =
            Addressables.UpdateCatalogs(catalogs, false);
        try
        {
            await update.Task;
            ThrowIfFailed(update, "load the new catalog");
        }
        finally
        {
            Release(update);
        }
        return catalogs.Count;
    }

    private async Task<long> DownloadAsync()
    {
        long bytes;
        AsyncOperationHandle<long> size = Addressables.GetDownloadSizeAsync((object)_label);
        try
        {
            bytes = await size.Task;
            ThrowIfFailed(size, $"size the '{_label}' content");
        }
        finally
        {
            Release(size);
        }

        if (bytes <= 0) return 0;

        AsyncOperationHandle download = Addressables.DownloadDependenciesAsync((object)_label, false);
        try
        {
            await download.Task;
            ThrowIfFailed(download, $"download the '{_label}' content");
        }
        finally
        {
            Release(download);
        }
        return bytes;
    }

    private static void ThrowIfFailed(AsyncOperationHandle handle, string what)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
            throw new InvalidOperationException($"Couldn't {what}: {handle.OperationException?.Message}");
    }

    private static void Release(AsyncOperationHandle handle)
    {
        if (handle.IsValid()) Addressables.Release(handle);
    }
}
