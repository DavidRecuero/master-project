/// <summary>
/// Reads and writes the player's vibration setting. Like the audio settings, it is a device setting,
/// not account data, so it lives in local storage and not in UserProfile.
/// </summary>
public class HapticsSettingsStore
{
    public const string EnabledKey = "haptics.enabled";

    private readonly IStorageProvider _storage;

    public HapticsSettingsStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    /// <summary>On by default.</summary>
    public bool Enabled
    {
        get
        {
            if (_storage == null || !_storage.HasKey(EnabledKey)) return true;
            return _storage.GetString(EnabledKey) == "1";
        }
        set
        {
            if (_storage == null) return;
            _storage.SetString(EnabledKey, value ? "1" : "0");
            _storage.Save();
        }
    }
}
