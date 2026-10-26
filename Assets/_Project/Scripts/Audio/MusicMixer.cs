using UnityEngine;

/// <summary>
/// The crossfade and ducking logic of the music, without any AudioSource: two slots that fade
/// in and out, plus a duck factor. AudioManager ticks it every frame and copies the volumes to
/// its sources. Pure C#, so it can be tested in EditMode.
/// </summary>
public class MusicMixer
{
    public enum RequestResult
    {
        /// <summary>Nothing to do (no clip).</summary>
        Ignored,
        /// <summary>The track was already playing; it keeps going.</summary>
        Continued,
        /// <summary>The track has to start on the active slot.</summary>
        Started
    }

    public class Slot
    {
        public AudioClip Clip;
        public float TrackVolume = 1f;
        public float Fade;        // current 0..1
        public float FadeTarget;  // where Fade is heading
        public bool IsAudible => Fade > 0f || FadeTarget > 0f;
    }

    public float CrossfadeDuration = 1.2f;
    public float DuckLevel = 0.25f;
    public float DuckFade = 0.25f;

    private readonly Slot[] _slots = { new Slot(), new Slot() };
    private float _duck = 1f;
    private float _duckUntil = float.NegativeInfinity;

    public int ActiveIndex { get; private set; }
    public Slot Active => _slots[ActiveIndex];
    public Slot GetSlot(int index) => _slots[index];
    public float Duck => _duck;

    /// <summary>
    /// Asks for a track. The same clip that is already playing (or fading out) keeps going;
    /// a different one goes on the other slot and the current one fades out.
    /// </summary>
    public RequestResult Request(AudioClip clip, float volume)
    {
        if (clip == null) return RequestResult.Ignored;

        Slot current = Active;
        if (current.Clip == clip && current.IsAudible)
        {
            current.TrackVolume = Mathf.Clamp01(volume);
            current.FadeTarget = 1f;
            return RequestResult.Continued;
        }

        current.FadeTarget = 0f;

        ActiveIndex = 1 - ActiveIndex;
        Slot next = Active;

        // Coming back to the track that is still fading out on the other slot: bring it back up, don't restart it
        if (next.Clip == clip && next.IsAudible)
        {
            next.TrackVolume = Mathf.Clamp01(volume);
            next.FadeTarget = 1f;
            return RequestResult.Continued;
        }

        next.Clip = clip;
        next.TrackVolume = Mathf.Clamp01(volume);
        next.Fade = 0f;
        next.FadeTarget = 1f;
        return RequestResult.Started;
    }

    public void Stop()
    {
        foreach (Slot slot in _slots) slot.FadeTarget = 0f;
    }

    public void RequestDuck(float now, float seconds)
    {
        _duckUntil = Mathf.Max(_duckUntil, now + Mathf.Max(0f, seconds));
    }

    /// <summary>Moves the fades and the duck towards their targets.</summary>
    public void Tick(float now, float deltaTime)
    {
        float duckTarget = now < _duckUntil ? DuckLevel : 1f;
        _duck = Mathf.MoveTowards(_duck, duckTarget, deltaTime / Mathf.Max(0.01f, DuckFade));

        float step = deltaTime / Mathf.Max(0.01f, CrossfadeDuration);
        foreach (Slot slot in _slots)
        {
            slot.Fade = Mathf.MoveTowards(slot.Fade, slot.FadeTarget, step);
        }
    }

    /// <summary>Final volume of a slot: its track volume, its fade, the duck and the player's music volume.</summary>
    public float VolumeOf(int index, float masterVolume)
    {
        Slot slot = _slots[index];
        return slot.TrackVolume * slot.Fade * _duck * Mathf.Clamp01(masterVolume);
    }

    /// <summary>A slot that has fully faded out can stop its source and drop the clip.</summary>
    public bool IsFinished(int index)
    {
        Slot slot = _slots[index];
        return slot.Clip != null && !slot.IsAudible;
    }

    public void Release(int index) => _slots[index].Clip = null;
}
