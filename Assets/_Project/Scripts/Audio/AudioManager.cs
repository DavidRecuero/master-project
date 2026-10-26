using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays the game's music and sound effects. Lives in the Boot scene and survives scene loads,
/// so the music keeps going (and crossfades) between the menu and the levels.
/// Two AudioSources alternate for the music crossfade (MusicMixer decides their volumes);
/// a small pool of sources plays the SFX so several can overlap without cutting each other.
/// </summary>
public class AudioManager : MonoBehaviour, IAudioService
{
    public static AudioManager Instance { get; private set; }

    [Header("Sounds")]
    [SerializeField] private SoundLibrary library;

    [Header("Mixer (optional)")]
    [Tooltip("Leave empty to play straight to the listener")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Music")]
    [Tooltip("Seconds to fade from one track to the next")]
    public float crossfadeDuration = 1.2f;
    [Tooltip("Music level while ducked under a jingle (0..1 of the normal level)")]
    [Range(0f, 1f)] public float duckLevel = 0.25f;
    [Tooltip("Seconds to go down into the duck and back up")]
    public float duckFade = 0.25f;

    [Header("SFX")]
    [Tooltip("How many effects can sound at the same time")]
    [Range(2, 16)] public int sfxVoices = 8;

    private readonly MusicMixer _mixer = new MusicMixer();
    private readonly AudioSource[] _musicSources = new AudioSource[2];
    private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
    private readonly List<float> _sfxStartTimes = new List<float>();
    private readonly SfxCooldowns _cooldowns = new SfxCooldowns();
    private readonly System.Random _rng = new System.Random();

    private AudioSettingsStore _settings;
    private bool _musicEnabled = true;
    private float _musicVolume = 1f;
    private bool _sfxEnabled = true;
    private float _sfxVolume = 1f;

    /// <summary>Optional injection (tests, or a different storage / library).</summary>
    public void Initialize(AudioSettingsStore settings, SoundLibrary soundLibrary = null)
    {
        _settings = settings;
        if (soundLibrary != null) library = soundLibrary;
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

        if (library == null) Debug.LogWarning("[AudioManager] No SoundLibrary assigned: sound effects won't play.");
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

        for (int i = 0; i < sfxVoices; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.outputAudioMixerGroup = sfxGroup;
            _sfxSources.Add(s);
            _sfxStartTimes.Add(float.NegativeInfinity);
        }
    }

    private void LoadSettings()
    {
        if (_settings == null) return;
        _musicEnabled = _settings.MusicEnabled;
        _musicVolume = _settings.MusicVolume;
        _sfxEnabled = _settings.SfxEnabled;
        _sfxVolume = _settings.SfxVolume;
    }

    // ---------- IAudioService: settings ----------

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

    public bool SfxEnabled
    {
        get => _sfxEnabled;
        set
        {
            _sfxEnabled = value;
            if (_settings != null) _settings.SfxEnabled = value;
            if (!value) StopAllSfx();
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set { _sfxVolume = Mathf.Clamp01(value); if (_settings != null) _settings.SfxVolume = _sfxVolume; }
    }

    // ---------- IAudioService: music ----------

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

    // ---------- IAudioService: SFX ----------

    public void PlaySfx(SfxId id, float pitchMultiplier = 1f)
    {
        if (!_sfxEnabled || library == null || id == SfxId.None) return;

        SoundEntry entry = library.Get(id);
        AudioClip clip = SoundLibrary.PickClip(entry, _rng);
        if (clip == null) return;

        if (!_cooldowns.TryPlay(id, Time.unscaledTime, entry.cooldown)) return;

        CreateSources();
        int voice = PickVoice();
        AudioSource s = _sfxSources[voice];
        s.clip = clip;
        s.volume = entry.volume * _sfxVolume;
        s.pitch = SoundLibrary.PickPitch(entry, _rng) * pitchMultiplier;
        s.Play();
        _sfxStartTimes[voice] = Time.unscaledTime;
    }

    // A free voice if there is one, otherwise the one that started longest ago
    private int PickVoice()
    {
        int oldest = 0;
        for (int i = 0; i < _sfxSources.Count; i++)
        {
            if (!_sfxSources[i].isPlaying) return i;
            if (_sfxStartTimes[i] < _sfxStartTimes[oldest]) oldest = i;
        }
        return oldest;
    }

    private void StopAllSfx()
    {
        foreach (AudioSource s in _sfxSources) s.Stop();
    }

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
