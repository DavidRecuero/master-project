using System;
using System.Globalization;

/// <summary>
/// The privacy choices saved on this device: one answer per purpose, the consent version they were given for,
/// and when the player last decided (GDPR asks to be able to show that consent was given).
/// A device setting like the audio or the language, not account data: it lives in local storage and not in
/// UserProfile, because it must be read before the sign-in and the cloud. A new device or a reinstall asks again.
/// </summary>
public class ConsentStore
{
    public const string StatusKeyPrefix = "privacy.consent.";
    public const string VersionKey = "privacy.consent_version";
    public const string DecisionKey = "privacy.consent_decision_ticks";

    private readonly IStorageProvider _storage;

    public ConsentStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    /// <summary>Key of one purpose, e.g. "privacy.consent.analytics".</summary>
    public static string StatusKey(ConsentPurpose purpose) => StatusKeyPrefix + purpose.ToString().ToLowerInvariant();

    /// <summary>The saved answer, whatever version it was given for. Unknown if there is none or it can't be read.</summary>
    public ConsentStatus GetStatus(ConsentPurpose purpose)
    {
        string raw = Read(StatusKey(purpose));
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) return ConsentStatus.Unknown;
        return value == (int)ConsentStatus.Granted || value == (int)ConsentStatus.Denied ? (ConsentStatus)value : ConsentStatus.Unknown;
    }

    /// <summary>Consent version of the last decision, 0 if the player never decided.</summary>
    public int Version => int.TryParse(Read(VersionKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(0, value) : 0;

    /// <summary>When the player last decided (UTC), or null if never.</summary>
    public DateTime? DecisionUtc
    {
        get
        {
            if (!long.TryParse(Read(DecisionKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) || ticks <= 0) return null;
            if (ticks > DateTime.MaxValue.Ticks) return null;
            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    /// <summary>Saves the answer of one purpose, with the version and the time of the decision.</summary>
    public void Save(ConsentPurpose purpose, ConsentStatus status, int version, DateTime utcNow)
    {
        if (_storage == null) return;
        _storage.SetString(StatusKey(purpose), ((int)status).ToString(CultureInfo.InvariantCulture));
        _storage.SetString(VersionKey, version.ToString(CultureInfo.InvariantCulture));
        _storage.SetString(DecisionKey, utcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        _storage.Save();
    }

    /// <summary>Forgets every answer (debug): the next launch asks again. IStorageProvider can't delete keys, so they are emptied.</summary>
    public void Clear()
    {
        if (_storage == null) return;
        foreach (ConsentPurpose purpose in Enum.GetValues(typeof(ConsentPurpose)))
            _storage.SetString(StatusKey(purpose), string.Empty);
        _storage.SetString(VersionKey, string.Empty);
        _storage.SetString(DecisionKey, string.Empty);
        _storage.Save();
    }

    private string Read(string key) => _storage != null && _storage.HasKey(key) ? _storage.GetString(key) : null;
}
