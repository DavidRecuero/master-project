/// <summary>
/// Saves the language the player chose by hand in Settings. Same idea as AudioSettingsStore:
/// a device setting, not account data, so it lives in local storage and not in UserProfile.
/// No saved value means "follow the device language".
/// </summary>
public class LanguageSettingsStore
{
    public const string LanguageKey = "settings.language";

    private readonly IStorageProvider _storage;

    public LanguageSettingsStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    /// <summary>True if the player chose a language by hand.</summary>
    public bool HasManualLanguage => !string.IsNullOrEmpty(Language);

    /// <summary>Locale code chosen by the player ("es"), or null when the device language decides.</summary>
    public string Language
    {
        get
        {
            if (_storage == null || !_storage.HasKey(LanguageKey)) return null;
            string code = _storage.GetString(LanguageKey);
            return string.IsNullOrEmpty(code) ? null : code;
        }
        set
        {
            if (_storage == null) return;
            _storage.SetString(LanguageKey, value ?? string.Empty);
            _storage.Save();
        }
    }

    /// <summary>Back to automatic. IStorageProvider has no delete, so an empty value means "none".</summary>
    public void Clear() => Language = null;
}
