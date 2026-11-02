using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

/// <summary>
/// ICloudSaveStore on top of Cloud Save (Player Data, default access class: only the player can read it).
/// Needs a signed-in player (Unity Authentication).
/// </summary>
public class UgsCloudSaveStore : ICloudSaveStore
{
    public async Task<string> LoadAsync(string key)
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
        return data.TryGetValue(key, out var item) ? item.Value.GetAs<string>() : null;
    }

    public async Task SaveAsync(string key, string value)
    {
        var data = new Dictionary<string, object> { { key, value } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }
}
