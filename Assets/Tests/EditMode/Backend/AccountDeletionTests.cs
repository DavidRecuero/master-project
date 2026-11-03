using System.Threading;
using NUnit.Framework;

public class AccountDeletionTests
{
    private FakeBackendAuthService _auth;
    private FakeCloudSaveStore _store;
    private FakeProfileRepository _repository;
    private CloudProfileSync _sync;
    private FakeUserDataProvider _profiles;
    private AccountDeletion _deletion;
    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        _auth = new FakeBackendAuthService();
        _auth.SignInNow();
        _store = new FakeCloudSaveStore();
        _repository = new FakeProfileRepository { Profile = new UserProfile { CurrentLevel = 6 } };
        _sync = new CloudProfileSync(_store, _repository);
        _sync.PullAsync(_auth.PlayerId).GetAwaiter().GetResult();   // signed in and in sync, as after the Boot
        _profiles = new FakeUserDataProvider { CurrentLevel = 6, Coins = 300 };

        _deletion = new AccountDeletion(_auth, _store, _sync, _profiles);
    }

    [TearDown]
    public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

    private AccountDeletionResult Delete() => _deletion.DeleteAsync().GetAwaiter().GetResult();

    [Test]
    public void Delete_RemovesTheCloudProfile_TheAccount_AndTheDeviceData()
    {
        Assert.AreEqual(AccountDeletionResult.Deleted, Delete());

        Assert.IsFalse(_store.Data.ContainsKey(CloudProfileSync.ProfileKey));
        Assert.IsTrue(_auth.AccountDeleted);
        Assert.IsTrue(_profiles.ResetDataCalled);
    }

    [Test]
    public void Delete_StopsTheUploads_SoTheResetIsNotUploaded()
    {
        Delete();

        _repository.SimulateGameSave(p => p.CurrentLevel = 1);   // the reset saves on the device

        Assert.IsFalse(_store.Data.ContainsKey(CloudProfileSync.ProfileKey), "The deleted account must not get data back");
    }

    [Test]
    public void Delete_WhenTheAccountDeletionFails_KeepsEverything_AndUploadsAgain()
    {
        _auth.FailDelete = true;

        Assert.AreEqual(AccountDeletionResult.Failed, Delete());

        Assert.IsFalse(_profiles.ResetDataCalled, "Nothing deleted on the device: the player can retry");
        Assert.IsFalse(_sync.IsStopped);
        Assert.IsTrue(_store.Data.ContainsKey(CloudProfileSync.ProfileKey), "The cloud copy is uploaded again");
    }

    [Test]
    public void Delete_GoesOn_WhenOnlyTheCloudProfileCantBeDeleted()
    {
        _store.FailDeletes = true;

        Assert.AreEqual(AccountDeletionResult.Deleted, Delete());
        Assert.IsTrue(_auth.AccountDeleted, "Unity deletes the player's data with the account anyway");
    }

    [Test]
    public void Delete_WhenNotSignedIn_DeletesNothing()
    {
        var offlineAuth = new FakeBackendAuthService();
        var deletion = new AccountDeletion(offlineAuth, _store, _sync, _profiles);

        Assert.IsFalse(deletion.IsAvailable);
        Assert.AreEqual(AccountDeletionResult.NotSignedIn, deletion.DeleteAsync().GetAwaiter().GetResult());
        Assert.IsFalse(_profiles.ResetDataCalled);
        Assert.AreEqual(0, _store.DeleteCount);
    }

    [Test]
    public void Delete_WithTheCloudOff_IsNotAvailable()
    {
        var deletion = new AccountDeletion(null, null, null, _profiles);

        Assert.IsFalse(deletion.IsAvailable);
        Assert.AreEqual(AccountDeletionResult.NotSignedIn, deletion.DeleteAsync().GetAwaiter().GetResult());
    }
}
