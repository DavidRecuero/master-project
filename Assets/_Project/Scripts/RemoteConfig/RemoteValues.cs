using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// A snapshot of the Remote Config settings: key → value as text (what the server sent, or the copy saved on the
/// device). Immutable, so a value read at the start of a level never changes in the middle of it.
/// Every getter takes the local value as fallback: a missing or unreadable key never breaks the game.
/// </summary>
public sealed class RemoteValues
{
    public static readonly RemoteValues Empty = new RemoteValues(null);

    private readonly Dictionary<string, string> _values;

    public RemoteValues(IDictionary<string, string> values)
    {
        _values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (values == null) return;

        foreach (KeyValuePair<string, string> pair in values)
        {
            if (string.IsNullOrEmpty(pair.Key) || pair.Value == null) continue;
            _values[pair.Key] = pair.Value;
        }
    }

    public int Count => _values.Count;

    public IEnumerable<string> Keys => _values.Keys;

    public bool Has(string key) => key != null && _values.ContainsKey(key);

    /// <summary>A copy of every key and value (to save it on the device).</summary>
    public Dictionary<string, string> ToDictionary() => new Dictionary<string, string>(_values, StringComparer.Ordinal);

    public string GetString(string key, string fallback) =>
        key != null && _values.TryGetValue(key, out string raw) ? raw : fallback;

    public int GetInt(string key, int fallback)
    {
        string raw = GetString(key, null);
        return raw != null && int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    public float GetFloat(string key, float fallback)
    {
        string raw = GetString(key, null);
        return raw != null && float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
               && !float.IsNaN(value) && !float.IsInfinity(value)
            ? value
            : fallback;
    }

    /// <summary>"true"/"false" in any case (the SDK sends "True"), or 1/0.</summary>
    public bool GetBool(string key, bool fallback)
    {
        string raw = GetString(key, null)?.Trim();
        if (string.IsNullOrEmpty(raw)) return fallback;
        if (bool.TryParse(raw, out bool value)) return value;
        if (raw == "1") return true;
        if (raw == "0") return false;
        return fallback;
    }
}
