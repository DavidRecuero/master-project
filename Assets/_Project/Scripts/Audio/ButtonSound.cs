using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a sound when the Button on this object is clicked. Add it next to any Button
/// (ButtonClick by default; PlayButton on the menu's Play). Disabled buttons don't click, so they stay silent.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [SerializeField] private SfxId sound = SfxId.ButtonClick;

    private Button _button;
    private IAudioService _audio;

    public SfxId Sound => sound;

    public void Initialize(IAudioService audio) => _audio = audio;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(Play);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(Play);
    }

    public void Play()
    {
        (_audio ?? AudioManager.Instance)?.PlaySfx(sound);
    }
}
