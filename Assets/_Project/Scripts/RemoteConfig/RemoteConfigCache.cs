using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// The last settings downloaded, saved on the device, so a launch without network plays with the last values the
/// player had and not with the ones in the build. A device setting like the consent: local storage, not UserProfile.
/// </summary>
public class RemoteConfigCache
{
    public const string ValuesKey = "remote_config.values";
    public const string FetchedAtKey = "remote_config.fetched_ticks";

    [Serializable]
    private class Entry
    {
        public string key;
        public string value;
    }

    [Serializable]
    private class Payload
    {
        public List<Entry> entries = new List<Entry>();
    }

    private readonly IStorageProvider _storage;

    public RemoteConfigCache(IStorageProvider storage)
    {
        _storage = storage;
    }

    /// <summary>The saved values, or Empty if there are none or they can't be read.</summary>
    public RemoteValues Load()
    {
        string json = Read(ValuesKey);
        if (string.IsNullOrEmpty(json)) return RemoteValues.Empty;

        try
        {
            Payload payload = JsonUtility.FromJson<Payload>(json);
            if (payload?.entries == null) return RemoteValues.Empty;

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in payload.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.value == null) continue;
                values[entry.key] = entry.value;
            }
            return new RemoteValues(values);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteConfig] Saved values can't be read, ignoring them: {e.Message}");
            return RemoteValues.Empty;
        }
    }

    /// <summary>When the saved values were downloaded (UTC), or null if never.</summary>
    public DateTime? FetchedAtUtc
    {
        get
        {
            if (!long.TryParse(Read(FetchedAtKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)) return null;
            if (ticks <= 0 || ticks > DateTime.MaxValue.Ticks) return null;
            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public void Save(RemoteValues values, DateTime utcNow)
    {
        if (_storage == null || values == null) return;

        var payload = new Payload();
        foreach (KeyValuePair<string, string> pair in values.ToDictionary())
            payload.entries.Add(new Entry { key = pair.Key, value = pair.Value });

        _storage.SetString(ValuesKey, JsonUtility.ToJson(payload));
        _storage.SetString(FetchedAtKey, utcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        _storage.Save();
    }

    /// <summary>Forgets the saved values (debug): the next launch without network uses the local config assets.</summary>
    public void Clear()
    {
        if (_storage == null) return;
        _storage.SetString(ValuesKey, string.Empty);
        _storage.SetString(FetchedAtKey, string.Empty);
        _storage.Save();
    }

    private string Read(string key) => _storage != null && _storage.HasKey(key) ? _storage.GetString(key) : null;
}
