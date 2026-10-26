using UnityEngine;

/// <summary>
/// Tells the AudioManager which track belongs to this scene. Put one in MainMenu and one in Level.
/// If the same track is already playing (e.g. "Play again" reloads the level), it just keeps going.
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)] public float volume = 0.7f;

    private IAudioService _audio;

    public AudioClip Music => music;

    public void Initialize(IAudioService audio, AudioClip clip = null)
    {
        _audio = audio;
        if (clip != null) music = clip;
    }

    private void Start()
    {
        _audio ??= AudioManager.Instance;

        if (_audio == null)
        {
            Debug.LogWarning("[SceneMusic] No AudioManager found. Play from the Boot scene to hear the music.");
            return;
        }

        _audio.PlayMusic(music, volume);
    }
}
