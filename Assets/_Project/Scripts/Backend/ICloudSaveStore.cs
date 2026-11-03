using System.Threading.Tasks;

/// <summary>
/// Key-value storage of the signed-in player's data on the backend (Cloud Save).
/// Values are plain strings (JSON), so the game code and the tests don't depend on the SDK.
/// Methods throw on failure.
/// </summary>
public interface ICloudSaveStore
{
    /// <summary>The stored value, or null when the key doesn't exist.</summary>
    Task<string> LoadAsync(string key);

    Task SaveAsync(string key, string value);

    Task DeleteAsync(string key);
}
