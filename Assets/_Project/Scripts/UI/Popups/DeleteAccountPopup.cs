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
/// When the account is deleted the game restarts from the Boot scene, with a new empty account.
/// </summary>
public class DeleteAccountPopup : PopupView
{
    public const int BootSceneIndex = 0;

    public enum State
    {
        Confirm,
        Connecting,
        Deleting,
        Offline,
        Failed
    }

    [SerializeField] private Button confirmButton;
    [Tooltip("HoldToConfirm on the confirm button: the deletion starts when the hold completes")]
    [SerializeField] private HoldToConfirm confirmHold;
    [Tooltip("While offline, seconds between connection retries (only while the popup is open)")]
    [SerializeField] private float retrySeconds = 3f;

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

    private IAccountDeletion _deletion;
    private ISceneLoader _sceneLoader;
    private ICloudConnection _connection;

    public State CurrentState { get; private set; }

    private Coroutine _retry;

    /// <summary>
    /// Optional injection (tests). Without it, the services created by the Boot and the real scene loader.
    /// connection: the cloud session, used to connect first if the game is offline (null: no retry).
    /// </summary>
    public void Initialize(IAccountDeletion deletion, ISceneLoader sceneLoader, ICloudConnection connection = null)
    {
        _deletion = deletion;
        _sceneLoader = sceneLoader;
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
        _sceneLoader ??= new UnitySceneLoader();
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

        if (state == State.Offline && _retry == null && _connection != null && Application.isPlaying && isActiveAndEnabled)
            _retry = StartCoroutine(RetryWhileOffline());

        // Hidden (not just disabled) when it can't be used: a red button always looks tappable
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(state == State.Confirm || state == State.Failed);

        if (bodyLabel != null) bodyLabel.StringReference = TextFor(state);
    }

    private LocalizedString TextFor(State state)
    {
        switch (state)
        {
            case State.Connecting: return connectingText;
            case State.Deleting: return deletingText;
            case State.Offline: return offlineText;
            case State.Failed: return failedText;
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
}
