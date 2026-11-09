using System;
using System.Globalization;

/// <summary>
/// How many times the review card was asked for on this device, and when the last time was.
/// A device setting like the language or the audio, not account data: it lives in local storage and not
/// in UserProfile (Google limits the card per account anyway, so a new device asking again is harmless).
/// </summary>
public class RateUsStore
{
    public const string PromptsKey = "rateus.prompts";
    public const string LastPromptKey = "rateus.last_prompt_ticks";

    private readonly IStorageProvider _storage;

    public RateUsStore(IStorageProvider storage)
    {
        _storage = storage;
    }

    /// <summary>Times the card was asked for (Google accepted the request).</summary>
    public int Prompts => int.TryParse(Read(PromptsKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(0, value) : 0;

    /// <summary>When it was last asked for (UTC), or null if never.</summary>
    public DateTime? LastPromptUtc
    {
        get
        {
            if (!long.TryParse(Read(LastPromptKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) || ticks <= 0) return null;
            if (ticks > DateTime.MaxValue.Ticks) return null;
            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public void RecordPrompt(DateTime utcNow)
    {
        if (_storage == null) return;
        _storage.SetString(PromptsKey, (Prompts + 1).ToString(CultureInfo.InvariantCulture));
        _storage.SetString(LastPromptKey, utcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        _storage.Save();
    }

    private string Read(string key) => _storage != null && _storage.HasKey(key) ? _storage.GetString(key) : null;
}
