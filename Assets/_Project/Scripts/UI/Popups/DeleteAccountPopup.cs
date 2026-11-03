using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Confirmation popup for "Delete account", opened from Settings.
/// Styled as a critical step (red title, red button). The X and the dim cancel. The button has to be
/// held down for a few seconds (HoldToConfirm) to delete, so it can't happen with a stray tap. States:
///   Confirm  - warning text, delete button shown.
///   Deleting - "deleting..." text, button hidden (no double taps).
///   Offline  - the game has no cloud session: explains that it needs internet, button hidden.
///   Failed   - the backend failed: error text, button shown to retry.
/// When the account is deleted the game restarts from the Boot scene, with a new empty account.
/// </summary>
public class DeleteAccountPopup : PopupView
{
    public const int BootSceneIndex = 0;

    public enum State
    {
        Confirm,
        Deleting,
        Offline,
        Failed
    }

    [SerializeField] private Button confirmButton;
    [Tooltip("HoldToConfirm on the confirm button: the deletion starts when the hold completes")]
    [SerializeField] private HoldToConfirm confirmHold;

    [Header("Texts")]
    [Tooltip("LocalizeStringEvent of the body text. The popup changes its string with the state.")]
    [SerializeField] private LocalizeStringEvent bodyLabel;
    [Tooltip("UI/delete.warning")]
    [SerializeField] private LocalizedString warningText = new LocalizedString();
    [Tooltip("UI/delete.deleting")]
    [SerializeField] private LocalizedString deletingText = new LocalizedString();
    [Tooltip("UI/delete.offline")]
    [SerializeField] private LocalizedString offlineText = new LocalizedString();
    [Tooltip("UI/delete.failed")]
    [SerializeField] private LocalizedString failedText = new LocalizedString();

    private IAccountDeletion _deletion;
    private ISceneLoader _sceneLoader;

    public State CurrentState { get; private set; }

    /// <summary>Optional injection (tests). Without it, the services created by the Boot and the real scene loader.</summary>
    public void Initialize(IAccountDeletion deletion, ISceneLoader sceneLoader)
    {
        _deletion = deletion;
        _sceneLoader = sceneLoader;
        ShowInitialState();
    }

    private void Awake()
    {
        if (confirmHold != null) confirmHold.Completed += OnConfirmClicked;
    }

    private void OnDestroy()
    {
        if (confirmHold != null) confirmHold.Completed -= OnConfirmClicked;
    }

    protected override void OnOpened()
    {
        _deletion ??= AccountDeletion.FromBackendServices(UserDataManager.Instance);
        _sceneLoader ??= new UnitySceneLoader();
        ShowInitialState();
    }

    private void ShowInitialState() => Show(_deletion != null && _deletion.IsAvailable ? State.Confirm : State.Offline);

    /// <summary>Called when the hold on the button completes.</summary>
    public async void OnConfirmClicked()
    {
        if (_deletion == null || CurrentState == State.Deleting) return;

        Show(State.Deleting);

        AccountDeletionResult result;
        try
        {
            result = await _deletion.DeleteAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DeleteAccountPopup] {e.Message}");
            result = AccountDeletionResult.Failed;
        }

        switch (result)
        {
            case AccountDeletionResult.Deleted:
                // Even if the popup was closed meanwhile: the game must restart on the new account
                _sceneLoader?.LoadScene(BootSceneIndex);
                break;
            case AccountDeletionResult.NotSignedIn:
                Show(State.Offline);
                break;
            default:
                Show(State.Failed);
                break;
        }
    }

    private void Show(State state)
    {
        CurrentState = state;
        if (this == null) return;   // closed while deleting

        // Hidden (not just disabled) when it can't be used: a red button always looks tappable
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(state == State.Confirm || state == State.Failed);

        if (bodyLabel != null) bodyLabel.StringReference = TextFor(state);
    }

    private LocalizedString TextFor(State state)
    {
        switch (state)
        {
            case State.Deleting: return deletingText;
            case State.Offline: return offlineText;
            case State.Failed: return failedText;
            default: return warningText;
        }
    }
}
