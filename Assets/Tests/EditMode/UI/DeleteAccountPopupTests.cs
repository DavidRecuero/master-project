using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>"Delete account" confirmation: states, and closing the game once the account is gone.</summary>
public class DeleteAccountPopupTests
{
    private GameObject _root;
    private DeleteAccountPopup _popup;
    private Button _confirmButton;
    private FakeAccountDeletion _deletion;
    private FakeAppQuitter _quitter;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("DeleteAccountPopup", typeof(RectTransform));
        _popup = _root.AddComponent<DeleteAccountPopup>();

        _confirmButton = new GameObject("ConfirmButton").AddComponent<Button>();
        _confirmButton.transform.SetParent(_root.transform);
        PopupTestUtils.SetField(_popup, "confirmButton", _confirmButton);

        _deletion = new FakeAccountDeletion();
        _quitter = new FakeAppQuitter();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void Opens_AskingForConfirmation_WithTheButtonShown()
    {
        _popup.Initialize(_deletion, _quitter);

        Assert.AreEqual(DeleteAccountPopup.State.Confirm, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(0, _deletion.Calls, "Opening deletes nothing");
    }

    [Test]
    public void Opens_Offline_WhenTheAccountCantBeDeletedNow()
    {
        _deletion.IsAvailable = false;

        _popup.Initialize(_deletion, _quitter);

        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
    }

    [Test]
    public void OpenedOffline_ConnectsFirst_ThenAsksForConfirmation()
    {
        _deletion.IsAvailable = false;
        var connection = new FakeCloudConnection { ConnectsOnNextTry = true };
        connection.OnConnected = () => _deletion.IsAvailable = true;

        _popup.Initialize(_deletion, _quitter, connection);

        Assert.AreEqual(1, connection.ConnectCalls);
        Assert.AreEqual(DeleteAccountPopup.State.Confirm, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
    }

    [Test]
    public void OpenedOffline_StillNoNetwork_ShowsOffline()
    {
        _deletion.IsAvailable = false;
        var connection = new FakeCloudConnection { ConnectsOnNextTry = false };

        _popup.Initialize(_deletion, _quitter, connection);

        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
    }

    [Test]
    public void WhileConnecting_TheButtonIsHidden_AndNothingIsDeleted()
    {
        _deletion.IsAvailable = false;
        var connection = new FakeCloudConnection { Pending = new TaskCompletionSource<bool>() };

        _popup.Initialize(_deletion, _quitter, connection);
        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Connecting, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(0, _deletion.Calls);
    }

    [Test]
    public void WhileOffline_ARetryThatConnects_ShowsTheConfirmation()
    {
        _deletion.IsAvailable = false;
        var connection = new FakeCloudConnection { ConnectsOnNextTry = false };
        _popup.Initialize(_deletion, _quitter, connection);
        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);

        connection.ConnectsOnNextTry = true;   // the network is back a few seconds later
        connection.OnConnected = () => _deletion.IsAvailable = true;
        _popup.TryReconnectAsync().GetAwaiter().GetResult();

        Assert.AreEqual(DeleteAccountPopup.State.Confirm, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
    }

    [Test]
    public void WhileOffline_ARetryWithoutNetwork_StaysOffline()
    {
        _deletion.IsAvailable = false;
        var connection = new FakeCloudConnection { ConnectsOnNextTry = false };
        _popup.Initialize(_deletion, _quitter, connection);

        _popup.TryReconnectAsync().GetAwaiter().GetResult();

        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);
        Assert.AreEqual(2, connection.ConnectCalls);
    }

    [Test]
    public void AlreadyConnected_DoesNotTryToConnectAgain()
    {
        var connection = new FakeCloudConnection { IsConnected = true };

        _popup.Initialize(_deletion, _quitter, connection);

        Assert.AreEqual(0, connection.ConnectCalls);
        Assert.AreEqual(DeleteAccountPopup.State.Confirm, _popup.CurrentState);
    }

    [Test]
    public void Confirm_Deletes_ShowsDeleted_AndClosesTheGame()
    {
        _popup.Initialize(_deletion, _quitter);

        _popup.OnConfirmClicked();

        Assert.AreEqual(1, _deletion.Calls);
        Assert.AreEqual(DeleteAccountPopup.State.Deleted, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(1, _quitter.QuitCalls);
    }

    [Test]
    public void TheGameClosesOnlyOnce()
    {
        _popup.Initialize(_deletion, _quitter);
        _popup.OnConfirmClicked();

        // The delayed close runs too (in the Editor tests it can't wait, so it already closed)
        var delayed = _popup.QuitAfterDelay();
        while (delayed.MoveNext()) { }

        Assert.AreEqual(1, _quitter.QuitCalls);
    }

    [Test]
    public void WhileDeletingAndOnceDeleted_ThePopupCantBeClosed()
    {
        _deletion.Pending = new TaskCompletionSource<AccountDeletionResult>();
        _popup.Initialize(_deletion, _quitter);
        Assert.IsTrue(_popup.CanClose, "Before confirming, the X and the dim cancel");

        _popup.OnConfirmClicked();
        Assert.IsFalse(_popup.CanClose, "Deleting");

        _deletion.Pending.SetResult(AccountDeletionResult.Deleted);
        Assert.IsFalse(_popup.CanClose, "Deleted");
    }

    [Test]
    public void AfterAFailure_ThePopupCanBeClosedAgain()
    {
        _deletion.Result = AccountDeletionResult.Failed;
        _popup.Initialize(_deletion, _quitter);

        _popup.OnConfirmClicked();

        Assert.IsTrue(_popup.CanClose);
        Assert.AreEqual(0, _quitter.QuitCalls);
    }

    [Test]
    public void WhileDeleting_TheButtonIsHidden_AndASecondTapDoesNothing()
    {
        _deletion.Pending = new TaskCompletionSource<AccountDeletionResult>();
        _popup.Initialize(_deletion, _quitter);

        _popup.OnConfirmClicked();
        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Deleting, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(1, _deletion.Calls);
        Assert.AreEqual(0, _quitter.QuitCalls);
    }

    [Test]
    public void Failure_ShowsTheError_AndLetsThePlayerRetry()
    {
        _deletion.Result = AccountDeletionResult.Failed;
        _popup.Initialize(_deletion, _quitter);

        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Failed, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(0, _quitter.QuitCalls, "No close: the account still exists");
    }

    [Test]
    public void ConnectionLostBeforeConfirming_ShowsOffline()
    {
        _deletion.Result = AccountDeletionResult.NotSignedIn;
        _popup.Initialize(_deletion, _quitter);

        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);
    }
}

/// <summary>Returns the result the test sets, right away or when the test completes Pending.</summary>
public class FakeAccountDeletion : IAccountDeletion
{
    public bool IsAvailable { get; set; } = true;
    public AccountDeletionResult Result { get; set; } = AccountDeletionResult.Deleted;
    public TaskCompletionSource<AccountDeletionResult> Pending { get; set; }
    public int Calls { get; private set; }

    public Task<AccountDeletionResult> DeleteAsync()
    {
        Calls++;
        return Pending != null ? Pending.Task : Task.FromResult(Result);
    }
}

/// <summary>Counts the times the game was told to close. Closes nothing.</summary>
public class FakeAppQuitter : IAppQuitter
{
    public int QuitCalls { get; private set; }

    public void Quit() => QuitCalls++;
}
