using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings button of the gameplay HUD (left of the coins). Opens the gameplay Settings popup
/// (audio, language and "Abandon level") through the PopupService. Put it on the button:
/// no OnClick to wire, like PopupCloseButton.
/// Once the level is won or lost the button stops working: the result popup is up and there is
/// no level left to abandon.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameplaySettingsButton : MonoBehaviour
{
    [Tooltip("GameplaySettingsPopup prefab")]
    [SerializeField] private SettingsPopup settingsPopupPrefab;

    private Button _button;
    private IPopupService _popupService;

    /// <summary>Optional injection (tests). Without it, the PopupService of the Boot scene is used.</summary>
    public void Initialize(IPopupService popupService)
    {
        _popupService = popupService;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(OnClicked);
        GameEvents.OnLevelCleared += HandleLevelEnded;
        GameEvents.OnLevelFailed += HandleLevelEnded;
        GameEvents.OnLevelAbandoned += HandleLevelEnded;
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(OnClicked);
        GameEvents.OnLevelCleared -= HandleLevelEnded;
        GameEvents.OnLevelFailed -= HandleLevelEnded;
        GameEvents.OnLevelAbandoned -= HandleLevelEnded;
    }

    private void HandleLevelEnded()
    {
        if (_button != null) _button.interactable = false;
    }

    public void OnClicked()
    {
        if (_button != null && !_button.interactable) return;

        if (settingsPopupPrefab == null)
        {
            Debug.LogWarning("[GameplaySettingsButton] No Settings popup prefab assigned.");
            return;
        }

        // Looked up on click: the PopupService comes from the Boot scene
        IPopupService popups = _popupService ?? PopupService.Instance;
        if (popups == null)
        {
            Debug.LogWarning("[GameplaySettingsButton] No PopupService found. Start from Boot scene.");
            return;
        }

        popups.Open(settingsPopupPrefab);
    }
}
