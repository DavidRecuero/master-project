using System.Collections.Generic;
using UnityEngine;

/// <summary>Records what the code under test asked of the audio system.</summary>
public class FakeAudioService : IAudioService
{
    public AudioClip LastMusic { get; private set; }
    public float LastMusicVolume { get; private set; }
    public int PlayMusicCalls { get; private set; }
    public int StopMusicCalls { get; private set; }
    public float LastDuckSeconds { get; private set; }

    /// <summary>Every PlaySfx call in order: the sound and the pitch multiplier it was asked with.</summary>
    public readonly List<(SfxId id, float pitch)> Sfx = new List<(SfxId id, float pitch)>();

    public bool MusicEnabled { get; set; } = true;
    public bool SfxEnabled { get; set; } = true;
    public float MusicVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        PlayMusicCalls++;
        LastMusic = clip;
        LastMusicVolume = volume;
    }

    public void StopMusic() => StopMusicCalls++;
    public void DuckMusic(float seconds) => LastDuckSeconds = seconds;
    public void PlaySfx(SfxId id, float pitchMultiplier = 1f) => Sfx.Add((id, pitchMultiplier));

    public int Count(SfxId id) => Sfx.FindAll(s => s.id == id).Count;
    public SfxId Last => Sfx.Count > 0 ? Sfx[Sfx.Count - 1].id : SfxId.None;
}
