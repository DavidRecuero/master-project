using System.Collections.Generic;

/// <summary>
/// Remembers when each sound last played, so the same effect can't pile up in one instant
/// (e.g. three items landing in the same frame). Pure C#, the caller passes the time.
/// </summary>
public class SfxCooldowns
{
    private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();

    /// <summary>True (and the play is recorded) if the cooldown has passed since the last play.</summary>
    public bool TryPlay(SfxId id, float now, float cooldown)
    {
        if (cooldown > 0f && _lastPlayed.TryGetValue(id, out float last) && now - last < cooldown)
        {
            return false;
        }

        _lastPlayed[id] = now;
        return true;
    }

    public void Clear() => _lastPlayed.Clear();
}
