using UnityEngine;

/// <summary>
/// Tells the AudioManager which track belongs to this scene. Put one in MainMenu and one in Level.
/// The track is a song of the music asset pack, by name (file name without .ogg, see MusicPack): it plays as soon
/// as MusicLibrary has it. Right after install the pack may still be downloading: the scene starts silent and
/// the music comes in when it lands (only if the player is still in this scene).
/// If the same track is already playing (e.g. "Play again" reloads the level), it just keeps going.
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [Tooltip("Song of the music pack: file name in MusicPack.androidpack/src/main/assets without .ogg (e.g. pleasant_creek)")]
    [SerializeField] private string track;
    [Range(0f, 1f)] public float volume = 0.7f;

    private IAudioService _audio;
    private IMusicLibrary _library;

    public string Track => track;

    public void Initialize(IAudioService audio, IMusicLibrary library = null, string trackName = null)
    {
        _audio = audio;
        if (library != null) _library = library;
        if (!string.IsNullOrEmpty(trackName)) track = trackName;
    }

    private void Start()
    {
        _audio ??= AudioManager.Instance;

        if (_audio == null)
        {
            Debug.LogWarning("[SceneMusic] No AudioManager found. Play from the Boot scene to hear the music.");
            return;
        }

        if (string.IsNullOrEmpty(track))
        {
            Debug.LogWarning($"[SceneMusic] No track set on {name}.");
            return;
        }

        _library ??= MusicLibrary.Instance;
        _library.Request(track, Play);
    }

    private void Play(AudioClip clip)
    {
        // Unity's == : the song may arrive after this scene was unloaded
        if (this == null || clip == null) return;

        _audio.PlayMusic(clip, volume);
    }
}
