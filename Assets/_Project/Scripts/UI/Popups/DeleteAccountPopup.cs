using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Confirmation popup for "Delete account", opened from Settings.
/// Styled as a critical step (red title, red button). The X and the dim cancel. The button has to be
/// held down for a few seconds (HoldToConfirm) to delete, so it can't happen with a stray tap. States:
///   Connecting - the game is offline this session: it tries to connect first ("connecting..." text, button hidden).
///   Confirm  - warning text, delete button shown.
///   Deleting - "deleting..." text, button hidden (no double taps).
///   Offline  - no connection: explains that it needs internet, button hidden, and keeps retrying every
///              retrySeconds while open (the network can take a few seconds to come back after airplane mode).
///   Failed   - the backend failed: error text, button shown to retry.
///   Deleted  - "account deleted, the game will close" text; after quitDelaySeconds the game closes.
///              Opened again, the Boot signs in to a new, empty account.
/// While Deleting and Deleted the popup can't be closed (no X, the dim and the back button do nothing).
/// </summary>
public class DeleteAccountPopup : PopupView
{
    public enum State
    {
        Confirm,
        Connecting,
        Deleting,
        Offline,
        Failed,
        Deleted
    }

    [SerializeField] private Button confirmButton;
    [Tooltip("HoldToConfirm on the confirm button: the deletion starts when the hold completes")]
    [SerializeField] private HoldToConfirm confirmHold;
    [Tooltip("While offline, seconds between connection retries (only while the popup is open)")]
    [SerializeField] private float retrySeconds = 3f;
    [Tooltip("Seconds the 'account deleted' message stays on screen before the game closes")]
    [SerializeField] private float quitDelaySeconds = 2f;

    [Header("Texts")]
    [Tooltip("LocalizeStringEvent of the body text. The popup changes its string with the state.")]
    [SerializeField] private LocalizeStringEvent bodyLabel;
    [Tooltip("UI/delete.connecting")]
    [SerializeField] private LocalizedString connectingText = new LocalizedString();
    [Tooltip("UI/delete.warning")]
    [SerializeField] private LocalizedString warningText = new LocalizedString();
    [Tooltip("UI/delete.deleting")]
    [SerializeField] private LocalizedString deletingText = new LocalizedString();
    [Tooltip("UI/delete.offline")]
    [SerializeField] private LocalizedString offlineText = new LocalizedString();
    [Tooltip("UI/delete.failed")]
    [SerializeField] private LocalizedString failedText = new LocalizedString();
    [Tooltip("UI/delete.done")]
    [SerializeField] private LocalizedString deletedText = new LocalizedString();

    private IAccountDeletion _deletion;
    private IAppQuitter _quitter;
    private ICloudConnection _connection;

    public State CurrentState { get; private set; }

    /// <summary>The player can't leave while the account is being deleted or once it's gone.</summary>
    public override bool CanClose => CurrentState != State.Deleting && CurrentState != State.Deleted;

    /// <summary>True once the game was told to close.</summary>
    public bool QuitRequested { get; private set; }

    private Coroutine _retry;

    /// <summary>
    /// Optional injection (tests). Without it, the services created by the Boot and the real app quitter.
    /// connection: the cloud session, used to connect first if the game is offline (null: no retry).
    /// </summary>
    public void Initialize(IAccountDeletion deletion, IAppQuitter quitter, ICloudConnection connection = null)
    {
        _deletion = deletion;
        _quitter = quitter;
        _connection = connection;
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
        _quitter ??= new UnityAppQuitter();
        _connection ??= BackendServices.Session;
        ShowInitialState();
    }

    /// <summary>Confirm if the account can be deleted now; otherwise try to connect first.</summary>
    private async void ShowInitialState()
    {
        if (_deletion != null && _deletion.IsAvailable)
        {
            Show(State.Confirm);
            return;
        }

        if (_deletion == null || _connection == null)
        {
            Show(State.Offline);
            return;
        }

        Show(State.Connecting);
        try
        {
            await _connection.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DeleteAccountPopup] {e.Message}");
        }

        if (CurrentState != State.Connecting) return;   // the player did something else meanwhile
        Show(_deletion.IsAvailable ? State.Confirm : State.Offline);
    }

    /// <summary>Called when the hold on the button completes.</summary>
    public async void OnConfirmClicked()
    {
        if (_deletion == null || CurrentState == State.Deleting || CurrentState == State.Connecting) return;

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
                Show(State.Deleted);
                if (Application.isPlaying && this != null && isActiveAndEnabled) StartCoroutine(QuitAfterDelay());
                else QuitNow();   // can't wait (tests, or the popup is gone): close right away
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

        if (state == State.Offline && _retry == null && _connection != null && Application.isPlaying && isActiveAndEnabled)
            _retry = StartCoroutine(RetryWhileOffline());

        // Hidden (not just disabled) when it can't be used: a red button always looks tappable
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(state == State.Confirm || state == State.Failed);

        if (bodyLabel != null) bodyLabel.StringReference = TextFor(state);

        // No X while it can't be closed (it would do nothing)
        foreach (PopupCloseButton close in GetComponentsInChildren<PopupCloseButton>(true))
            close.gameObject.SetActive(CanClose);
    }

    private LocalizedString TextFor(State state)
    {
        switch (state)
        {
            case State.Connecting: return connectingText;
            case State.Deleting: return deletingText;
            case State.Offline: return offlineText;
            case State.Failed: return failedText;
            case State.Deleted: return deletedText;
            default: return warningText;
        }
    }

    private IEnumerator RetryWhileOffline()
    {
        var wait = new WaitForSecondsRealtime(retrySeconds);
        while (CurrentState == State.Offline)
        {
            yield return wait;
            if (CurrentState != State.Offline) break;

            Task attempt = TryReconnectAsync();
            yield return new WaitUntil(() => attempt.IsCompleted);
        }
        _retry = null;
    }

    /// <summary>One connection retry while offline; goes to Confirm if the account can be deleted now. Public for the tests.</summary>
    public async Task TryReconnectAsync()
    {
        if (_connection == null || _deletion == null) return;

        try
        {
            await _connection.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DeleteAccountPopup] {e.Message}");
        }

        if (CurrentState == State.Offline && _deletion.IsAvailable) Show(State.Confirm);
    }

    /// <summary>Leaves the "account deleted" message on screen for quitDelaySeconds, then closes the game.</summary>
    public IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(quitDelaySeconds);
        QuitNow();
    }

    private void QuitNow()
    {
        if (QuitRequested) return;
        QuitRequested = true;
        _quitter?.Quit();
    }
}
