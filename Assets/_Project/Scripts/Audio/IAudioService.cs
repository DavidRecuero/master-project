using UnityEngine;

/// <summary>
/// What the game asks of the audio system. Scene, gameplay and UI code talk to this interface only,
/// so tests can swap in a fake and nothing depends on AudioSources directly.
/// </summary>
public interface IAudioService
{
    /// <summary>Starts a looping music track with a crossfade. Does nothing if that clip is already playing.</summary>
    void PlayMusic(AudioClip clip, float volume = 1f);

    /// <summary>Fades the music out.</summary>
    void StopMusic();

    /// <summary>Lowers the music for a few seconds (e.g. under the win/lose jingle), then brings it back.</summary>
    void DuckMusic(float seconds);

    /// <summary>Plays a sound effect. pitchMultiplier stacks on the entry's own random pitch (used for combos).</summary>
    void PlaySfx(SfxId id, float pitchMultiplier = 1f);

    bool MusicEnabled { get; set; }
    bool SfxEnabled { get; set; }

    /// <summary>0..1</summary>
    float MusicVolume { get; set; }

    /// <summary>0..1</summary>
    float SfxVolume { get; set; }
}
