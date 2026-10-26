using UnityEngine;

/// <summary>
/// Plays a sound every time this object is shown. Put it on a popup root (next to PopupAppear)
/// with PopupOpen. Objects that are already active when the scene loads stay silent on load,
/// unless playOnSceneStart is on.
/// </summary>
public class SoundOnEnable : MonoBehaviour
{
    [SerializeField] private SfxId sound = SfxId.PopupOpen;
    [Tooltip("Also play when the scene loads with this object already active")]
    [SerializeField] private bool playOnSceneStart;

    // Enables this close to the scene load count as "part of loading", not as the popup being shown
    private const float SceneLoadGrace = 0.05f;

    private IAudioService _audio;

    public void Initialize(IAudioService audio) => _audio = audio;

    private void OnEnable()
    {
        if (!ShouldPlay(Time.timeSinceLevelLoad, playOnSceneStart)) return;
        (_audio ?? AudioManager.Instance)?.PlaySfx(sound);
    }

    public static bool ShouldPlay(float timeSinceSceneLoad, bool playOnSceneStart)
    {
        return playOnSceneStart || timeSinceSceneLoad > SceneLoadGrace;
    }
}
