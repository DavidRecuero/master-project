using System.Globalization;

/// <summary>
/// Reads and writes the player's audio settings (music on/off and volume).
/// These are device settings, not account data, so they live in local storage and not in UserProfile.
/// </summary>
public class AudioSettingsStore
{
    public const string MusicEnabledKey = "audio.music.enabled";
    public const string MusicVolumeKey = "audio.music.volume";

    private readonly IStorageProvider _storage;

    public AudioSettingsStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    public bool MusicEnabled
    {
        get => GetBool(MusicEnabledKey, true);
        set => SetBool(MusicEnabledKey, value);
    }

    public float MusicVolume
    {
        get => GetFloat(MusicVolumeKey, 1f);
        set => SetFloat(MusicVolumeKey, value);
    }

    private bool GetBool(string key, bool fallback)
    {
        if (_storage == null || !_storage.HasKey(key)) return fallback;
        return _storage.GetString(key) == "1";
    }

    private void SetBool(string key, bool value)
    {
        if (_storage == null) return;
        _storage.SetString(key, value ? "1" : "0");
        _storage.Save();
    }

    // Invariant culture: a Spanish device would otherwise save "0,5" and fail to read it back elsewhere
    private float GetFloat(string key, float fallback)
    {
        if (_storage == null || !_storage.HasKey(key)) return fallback;
        return float.TryParse(_storage.GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? Clamp01(v)
            : fallback;
    }

    private void SetFloat(string key, float value)
    {
        if (_storage == null) return;
        _storage.SetString(key, Clamp01(value).ToString(CultureInfo.InvariantCulture));
        _storage.Save();
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
