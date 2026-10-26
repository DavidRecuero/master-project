using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays the game's music. Lives in the Boot scene and survives scene loads, so the music keeps
/// going (and crossfades) between the menu and the levels. Two AudioSources alternate for the
/// crossfade; MusicMixer decides their volumes.
/// </summary>
public class AudioManager : MonoBehaviour, IAudioService
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer (optional)")]
    [Tooltip("Leave empty to play straight to the listener")]
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Music")]
    [Tooltip("Seconds to fade from one track to the next")]
    public float crossfadeDuration = 1.2f;
    [Tooltip("Music level while ducked under a jingle (0..1 of the normal level)")]
    [Range(0f, 1f)] public float duckLevel = 0.25f;
    [Tooltip("Seconds to go down into the duck and back up")]
    public float duckFade = 0.25f;

    private readonly MusicMixer _mixer = new MusicMixer();
    private readonly AudioSource[] _musicSources = new AudioSource[2];

    private AudioSettingsStore _settings;
    private bool _musicEnabled = true;
    private float _musicVolume = 1f;

    /// <summary>Optional injection (tests, or a different storage).</summary>
    public void Initialize(AudioSettingsStore settings)
    {
        _settings = settings;
        LoadSettings();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        CreateSources();

        _settings ??= new AudioSettingsStore(new PlayerPrefsStorageProvider());
        LoadSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void CreateSources()
    {
        if (_musicSources[0] != null) return;

        for (int i = 0; i < _musicSources.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.volume = 0f;
            s.priority = 0; // Highest priority: Unity never steals the music's voice
            s.outputAudioMixerGroup = musicGroup;
            _musicSources[i] = s;
        }
    }

    private void LoadSettings()
    {
        if (_settings == null) return;
        _musicEnabled = _settings.MusicEnabled;
        _musicVolume = _settings.MusicVolume;
    }

    // ---------- IAudioService ----------

    public bool MusicEnabled
    {
        get => _musicEnabled;
        set { _musicEnabled = value; if (_settings != null) _settings.MusicEnabled = value; }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set { _musicVolume = Mathf.Clamp01(value); if (_settings != null) _settings.MusicVolume = _musicVolume; }
    }

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        CreateSources();

        if (_mixer.Request(clip, volume) != MusicMixer.RequestResult.Started) return;

        AudioSource source = _musicSources[_mixer.ActiveIndex];
        source.clip = clip;
        source.volume = 0f;
        source.Play();
    }

    public void StopMusic() => _mixer.Stop();

    public void DuckMusic(float seconds) => _mixer.RequestDuck(Time.unscaledTime, seconds);

    // ---------- Fades ----------

    private void Update()
    {
        // Unscaled: the music keeps fading even if the game is paused with timeScale 0
        _mixer.CrossfadeDuration = crossfadeDuration;
        _mixer.DuckLevel = duckLevel;
        _mixer.DuckFade = duckFade;
        _mixer.Tick(Time.unscaledTime, Time.unscaledDeltaTime);

        float master = _musicEnabled ? _musicVolume : 0f;

        for (int i = 0; i < _musicSources.Length; i++)
        {
            AudioSource source = _musicSources[i];
            if (source == null) continue;

            source.volume = _mixer.VolumeOf(i, master);

            // A track that has faded out stops, so it doesn't keep streaming in the background
            if (_mixer.IsFinished(i))
            {
                source.Stop();
                source.clip = null;
                _mixer.Release(i);
            }
        }
    }
}
