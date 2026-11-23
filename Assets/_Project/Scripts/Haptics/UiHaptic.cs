using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The smallest tick when the player taps a Button, or flips a Toggle, on this object.
/// It sits on the base button prefabs (Primary, Secondary, Icon, Close, coins and lives pills), so every
/// button made from them has it, and on the Settings switches. Same rule as ButtonSound: a disabled
/// button doesn't click, so it doesn't vibrate either. A Toggle only vibrates on a player change
/// (SetIsOnWithoutNotify, used when a popup shows the saved state, fires nothing).
/// Left out on purpose: the booster buttons (their use already vibrates through the gameplay router),
/// the dim background behind popups and the text links, which are silent for audio too.
/// </summary>
public class UiHaptic : MonoBehaviour
{
    [SerializeField] private HapticType haptic = HapticType.Selection;

    private Button _button;
    private Toggle _toggle;
    private IHapticsService _haptics;

    public HapticType Haptic => haptic;

    public void Initialize(IHapticsService haptics) => _haptics = haptics;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _toggle = GetComponent<Toggle>();

        if (_button == null && _toggle == null)
            Debug.LogWarning($"[UiHaptic] '{name}' has no Button or Toggle: it will never vibrate.", this);
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(Play);
        if (_toggle != null) _toggle.onValueChanged.AddListener(OnToggled);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(Play);
        if (_toggle != null) _toggle.onValueChanged.RemoveListener(OnToggled);
    }

    private void OnToggled(bool isOn) => Play();

    public void Play()
    {
        (_haptics ?? HapticsManager.Instance)?.Play(haptic);
    }
}
