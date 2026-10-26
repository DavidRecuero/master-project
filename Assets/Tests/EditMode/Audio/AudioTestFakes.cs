using UnityEngine;

/// <summary>Records what the code under test asked of the audio system.</summary>
public class FakeAudioService : IAudioService
{
    public AudioClip LastMusic { get; private set; }
    public float LastMusicVolume { get; private set; }
    public int PlayMusicCalls { get; private set; }
    public int StopMusicCalls { get; private set; }
    public float LastDuckSeconds { get; private set; }

    public bool MusicEnabled { get; set; } = true;
    public float MusicVolume { get; set; } = 1f;

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        PlayMusicCalls++;
        LastMusic = clip;
        LastMusicVolume = volume;
    }

    public void StopMusic() => StopMusicCalls++;
    public void DuckMusic(float seconds) => LastDuckSeconds = seconds;
}
