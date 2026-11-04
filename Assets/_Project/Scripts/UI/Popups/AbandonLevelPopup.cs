using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Confirmation popup for "Abandon level", opened from the gameplay Settings.
/// Warns that leaving counts as losing the level and lets the player choose:
///   Abandon      - the level is lost and the game goes back to the main menu (ILevelAbandoner).
///   Keep playing - closes every popup (this one and Settings) and the level goes on where it was.
/// The X, the dim and the back button only close this popup, back to Settings, like any other popup.
/// Once the player confirms, the popup can't be closed: the menu is loading.
/// The texts are fixed (LocalizeStringEvent in the prefab), so nothing is translated from here.
/// </summary>
public class AbandonLevelPopup : PopupView
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button keepPlayingButton;

    private ILevelAbandoner _abandoner;

    /// <summary>True once the player confirmed and the level was abandoned.</summary>
    public bool Abandoned { get; private set; }

    /// <summary>Nothing to go back to once the level is abandoned (the menu is loading).</summary>
    public override bool CanClose => !Abandoned;

    /// <summary>Optional injection (tests). Without it, the GameManager of the scene is used.</summary>
    public void Initialize(ILevelAbandoner abandoner)
    {
        _abandoner = abandoner;
    }

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (keepPlayingButton != null) keepPlayingButton.onClick.AddListener(OnKeepPlayingClicked);
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        if (keepPlayingButton != null) keepPlayingButton.onClick.RemoveListener(OnKeepPlayingClicked);
    }

    protected override void OnOpened()
    {
        _abandoner ??= GameManager.Instance;
        if (_abandoner == null) Debug.LogWarning("[AbandonLevelPopup] No GameManager found. Open it from the Level scene.");
    }

    /// <summary>Abandons the level. The scene change closes every popup.</summary>
    public void OnConfirmClicked()
    {
        if (Abandoned) return;   // double tap while the menu loads

        if (_abandoner != null && _abandoner.AbandonLevel())
        {
            Abandoned = true;
            SetButtonsInteractable(false);
            return;
        }

        // The level had already ended (e.g. won while the popup was open): nothing to abandon, back to the game
        CloseEverything();
    }

    /// <summary>Back to the level in progress: closes this popup and the Settings below it.</summary>
    public void OnKeepPlayingClicked()
    {
        if (Abandoned) return;
        CloseEverything();
    }

    private void CloseEverything()
    {
        if (Service != null) Service.CloseAll();
        else Close();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (confirmButton != null) confirmButton.interactable = interactable;
        if (keepPlayingButton != null) keepPlayingButton.interactable = interactable;

        // No X while it can't be closed (it would do nothing)
        foreach (PopupCloseButton close in GetComponentsInChildren<PopupCloseButton>(true))
            close.gameObject.SetActive(interactable);
    }
}
