using System.Threading;
using NUnit.Framework;

public class CloudBootstrapTests
{
    private FakeBackendAuthService _auth;
    private FakeCloudSaveStore _store;
    private FakeProfileRepository _profiles;
    private CloudProfileSync _sync;
    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        _auth = new FakeBackendAuthService { AccountId = "ugs-123" };
        _store = new FakeCloudSaveStore();
        _profiles = new FakeProfileRepository();
        _sync = new CloudProfileSync(_store, _profiles);
    }

    [TearDown]
    public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

    private bool Run(string code = null) =>
        new CloudBootstrap(_auth, _sync, "development").RunAsync(code).GetAwaiter().GetResult();

    [Test]
    public void Run_InitializesOnTheGivenEnvironment_SignsIn_AndSyncs()
    {
        Assert.IsTrue(Run());

        Assert.AreEqual("development", _auth.Environment);
        Assert.IsTrue(_auth.SignInCalled);
        Assert.AreEqual("ugs-123", _profiles.Profile.UserId);
        Assert.IsTrue(_sync.CanUpload);
    }

    [Test]
    public void Run_PassesThePlayGamesCode()
    {
        Run("one-time-code");

        Assert.AreEqual("one-time-code", _auth.CodeUsed);
    }

    [Test]
    public void Run_WithoutCode_SignsInAsGuest()
    {
        Run();

        Assert.IsNull(_auth.CodeUsed);
    }

    [Test]
    public void Run_WhenTheServicesFailToStart_GoesOffline_WithoutThrowing()
    {
        _auth.FailInitialize = true;

        Assert.IsFalse(Run());
        Assert.IsFalse(_auth.SignInCalled);
        Assert.IsFalse(_sync.CanUpload);
    }

    [Test]
    public void Run_WhenSignInFails_GoesOffline_AndUploadsNothing()
    {
        _auth.FailSignIn = true;

        Assert.IsFalse(Run());
        Assert.AreEqual(0, _store.SaveCount);
    }

    [Test]
    public void Run_WhenTheCloudCantBeRead_GoesOffline()
    {
        _store.FailLoads = true;

        Assert.IsFalse(Run());
        Assert.IsFalse(_sync.CanUpload);
    }
}
