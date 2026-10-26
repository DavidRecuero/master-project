using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-tunable settings of one sound effect: its clips (one is picked at random each time),
/// volume, random pitch range and a cooldown so it can't stack up in the same instant.
/// </summary>
[System.Serializable]
public class SoundEntry
{
    public SfxId id;
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Random pitch range. 1-1 = no variation. 0.96-1.04 keeps repeated sounds from feeling robotic.")]
    public Vector2 pitchRange = Vector2.one;
    [Tooltip("Minimum seconds between two plays of this sound. 0 = no limit.")]
    public float cooldown = 0.03f;

    public bool HasClips => clips != null && clips.Length > 0;
}

/// <summary>
/// The table of every sound effect in the game. One asset holds them all, so swapping a sound
/// (or a whole sound style) is a matter of changing clips here, never code.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Pipe Mess/Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private List<SoundEntry> entries = new List<SoundEntry>();

    public IReadOnlyList<SoundEntry> Entries => entries;

    /// <summary>The entry for this id, or null if the library has none.</summary>
    public SoundEntry Get(SfxId id)
    {
        if (id == SfxId.None) return null;

        foreach (SoundEntry entry in entries)
        {
            if (entry != null && entry.id == id) return entry;
        }
        return null;
    }

    /// <summary>Returns the entry for this id, creating an empty one if needed. Used by the editor tool.</summary>
    public SoundEntry GetOrCreate(SfxId id)
    {
        SoundEntry entry = Get(id);
        if (entry != null) return entry;

        entry = new SoundEntry { id = id };
        entries.Add(entry);
        return entry;
    }

    /// <summary>Picks one of the entry's clips (null if it has none).</summary>
    public static AudioClip PickClip(SoundEntry entry, System.Random rng)
    {
        if (entry == null || !entry.HasClips) return null;
        if (entry.clips.Length == 1) return entry.clips[0];
        return entry.clips[rng.Next(entry.clips.Length)];
    }

    /// <summary>A random pitch inside the entry's range.</summary>
    public static float PickPitch(SoundEntry entry, System.Random rng)
    {
        if (entry == null) return 1f;
        float min = Mathf.Min(entry.pitchRange.x, entry.pitchRange.y);
        float max = Mathf.Max(entry.pitchRange.x, entry.pitchRange.y);
        if (Mathf.Approximately(min, max)) return min <= 0f ? 1f : min;
        return min + (float)rng.NextDouble() * (max - min);
    }
}
