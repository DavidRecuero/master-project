using UnityEngine;

/// <summary>
/// What the game asks of the audio system. Scene and UI code talk to this interface only,
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

    bool MusicEnabled { get; set; }

    /// <summary>0..1</summary>
    float MusicVolume { get; set; }
}
