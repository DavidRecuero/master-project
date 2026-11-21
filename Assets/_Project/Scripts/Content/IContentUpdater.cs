using System.Threading.Tasks;

/// <summary>What a content update brought: new catalogs (new content published) and bytes downloaded.</summary>
public readonly struct ContentUpdateResult
{
    public readonly int CatalogsUpdated;
    public readonly long BytesDownloaded;

    public ContentUpdateResult(int catalogsUpdated, long bytesDownloaded)
    {
        CatalogsUpdated = catalogsUpdated;
        BytesDownloaded = bytesDownloaded;
    }

    public override string ToString() =>
        CatalogsUpdated == 0 && BytesDownloaded == 0
            ? "no new content"
            : $"{CatalogsUpdated} catalog(s) updated, {BytesDownloaded / 1024f:0.#} KB downloaded";
}

/// <summary>
/// Brings the remote content up to date before the game reads it: the newest catalog published on CCD and the
/// level bundles it lists. Throws if it can't (no network, no bucket yet): the game goes on with what it has.
/// </summary>
public interface IContentUpdater
{
    Task<ContentUpdateResult> UpdateAsync();
}
