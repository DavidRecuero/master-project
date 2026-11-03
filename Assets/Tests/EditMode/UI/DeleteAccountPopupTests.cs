using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>"Delete account" confirmation: states, and the restart from Boot once the account is gone.</summary>
public class DeleteAccountPopupTests
{
    private GameObject _root;
    private DeleteAccountPopup _popup;
    private Button _confirmButton;
    private FakeAccountDeletion _deletion;
    private FakeSceneLoader _sceneLoader;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("DeleteAccountPopup", typeof(RectTransform));
        _popup = _root.AddComponent<DeleteAccountPopup>();

        _confirmButton = new GameObject("ConfirmButton").AddComponent<Button>();
        _confirmButton.transform.SetParent(_root.transform);
        PopupTestUtils.SetField(_popup, "confirmButton", _confirmButton);

        _deletion = new FakeAccountDeletion();
        _sceneLoader = new FakeSceneLoader();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void Opens_AskingForConfirmation_WithTheButtonShown()
    {
        _popup.Initialize(_deletion, _sceneLoader);

        Assert.AreEqual(DeleteAccountPopup.State.Confirm, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(0, _deletion.Calls, "Opening deletes nothing");
    }

    [Test]
    public void Opens_Offline_WhenTheAccountCantBeDeletedNow()
    {
        _deletion.IsAvailable = false;

        _popup.Initialize(_deletion, _sceneLoader);

        Assert.AreEqual(DeleteAccountPopup.State.Offline, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
    }

    [Test]
    public void Confirm_DeletesAndRestartsFromBoot()
    {
        _popup.Initialize(_deletion, _sceneLoader);

        _popup.OnConfirmClicked();

        Assert.AreEqual(1, _deletion.Calls);
        Assert.AreEqual(DeleteAccountPopup.BootSceneIndex, _sceneLoader.LoadedSceneIndex);
    }

    [Test]
    public void WhileDeleting_TheButtonIsHidden_AndASecondTapDoesNothing()
    {
        _deletion.Pending = new TaskCompletionSource<AccountDeletionResult>();
        _popup.Initialize(_deletion, _sceneLoader);

        _popup.OnConfirmClicked();
        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Deleting, _popup.CurrentState);
        Assert.IsFalse(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(1, _deletion.Calls);
        Assert.AreEqual(-1, _sceneLoader.LoadedSceneIndex);
    }

    [Test]
    public void Failure_ShowsTheError_AndLetsThePlayerRetry()
    {
        _deletion.Result = AccountDeletionResult.Failed;
        _popup.Initialize(_deletion, _sceneLoader);

        _popup.OnConfirmClicked();

        Assert.AreEqual(DeleteAccountPopup.State.Failed, _popup.CurrentState);
        Assert.IsTrue(_confirmButton.gameObject.activeSelf);
        Assert.AreEqual(-1, _sceneLoader.LoadedSceneIndex, "No restart: the account still exists");
    }

    [Test]
    public void ConnectionLostBeforeConfirming_ShowsOffline()
    {
        _deletion.Result = AccountDeletionResult.NotSignedIn;
        _popup.Initialize(_deletion, _sceneLoader);

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
