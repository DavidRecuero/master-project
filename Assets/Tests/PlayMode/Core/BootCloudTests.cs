using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The cloud step of the Boot: sign-in on the backend, profile sync, and that it never blocks the menu.</summary>
public class BootCloudTests
{
    private GameObject _bootGO;
    private BootManager _boot;
    private FakeSceneLoader _sceneLoader;
    private BootFakeBackendAuth _backend;
    private BootFakeCloudStore _store;
    private BootFakeProfiles _profiles;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();
        BackendServices.Reset();

        _sceneLoader = new FakeSceneLoader();
        _backend = new BootFakeBackendAuth();
        _store = new BootFakeCloudStore();
        _profiles = new BootFakeProfiles();
    }

    [TearDown]
    public void TearDown()
    {
        if (_bootGO != null) UnityEngine.Object.Destroy(_bootGO);
        var reconnector = UnityEngine.Object.FindFirstObjectByType<CloudReconnector>();
        if (reconnector != null) UnityEngine.Object.Destroy(reconnector.gameObject);
        PlayerSession.Reset();
        BackendServices.Reset();
    }

    /// <summary>
    /// Creates the BootManager and injects everything in the same frame, before its Start runs.
    /// (Created in SetUp, Unity can run Start before the test body: it would boot with the real services.)
    /// </summary>
    private void Init(IAuthProvider auth, float timeout = 2f, bool enabled = true)
    {
        _bootGO = new GameObject("Boot");
        _boot = _bootGO.AddComponent<BootManager>();
        _boot.Initialize(_sceneLoader, auth, new FakeBootLocalization());
        _boot.InitializeCloud(_backend, _store, _profiles, timeout, enabled);
    }

    [UnityTest]
    public IEnumerator Guest_SignsInAnonymously_AndSyncs_BeforeTheMenu()
    {
        Init(new FakeAuthProvider());

        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(_backend.SignInCalled);
        Assert.IsNull(_backend.CodeUsed, "No Play Games code: anonymous sign-in");
        Assert.AreEqual("ugs-player", _profiles.Profile.UserId);
        Assert.IsNotNull(_store.LastSaved, "The device copy was uploaded");
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
        Assert.IsNotNull(BackendServices.ProfileSync, "Shared for the rest of the game");
    }

    [UnityTest]
    public IEnumerator PlayGamesPlayer_SignsInWithTheServerCode()
    {
        Init(new FakePlayGamesAuth { Code = "one-time-code" });

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual("one-time-code", _backend.CodeUsed);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator PlayGamesPlayer_WithoutCode_SkipsTheCloud_NeverAsGuest()
    {
        Init(new FakePlayGamesAuth { Code = null });

        yield return new WaitForSeconds(0.2f);

        Assert.IsFalse(_backend.SignInCalled);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator SlowBackend_DoesNotBlockTheMenu()
    {
        _backend.NeverFinishSignIn = true;
        Init(new FakeAuthProvider(), timeout: 0.3f);

        yield return new WaitForSeconds(0.15f);
        Assert.AreEqual(-1, _sceneLoader.LoadedSceneIndex, "Still waiting for the cloud");

        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex, "Menu opens with the data on the device");
        Assert.IsTrue(BackendServices.ProfileSync.ProfileInUse);
    }

    [UnityTest]
    public IEnumerator AfterTheBoot_TheProfileInUseIsLocked()
    {
        Init(new FakeAuthProvider());

        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(BackendServices.ProfileSync.ProfileInUse, "A later cloud copy must never replace the profile mid-session");
    }

    [UnityTest]
    public IEnumerator StartedOffline_TheSessionConnectsLater()
    {
        var playGames = new FakePlayGamesAuth { Code = null };   // offline: Play Games gives no code
        Init(playGames);

        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
        Assert.IsFalse(_backend.SignInCalled);

        playGames.Code = "code-later";   // network is back
        var reconnect = BackendServices.Session.ConnectAsync();
        yield return new WaitUntil(() => reconnect.IsCompleted);

        Assert.IsTrue(reconnect.Result);
        Assert.AreEqual("code-later", _backend.CodeUsed);
        Assert.IsNotNull(_store.LastSaved, "The progress made offline is uploaded");
    }

    [UnityTest]
    public IEnumerator FailingBackend_StillLoadsTheMenu()
    {
        _backend.FailSignIn = true;
        Init(new FakeAuthProvider());

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
        Assert.IsNull(_store.LastSaved);
    }

    [UnityTest]
    public IEnumerator CloudOff_NeverTouchesTheBackend()
    {
        Init(new FakeAuthProvider(), enabled: false);

        yield return new WaitForSeconds(0.2f);

        Assert.IsFalse(_backend.SignInCalled);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }
}

/// <summary>Play Games sign-in that also hands out a server code.</summary>
public class FakePlayGamesAuth : FakeAuthProvider, IServerAuthCodeProvider
{
    public string Code { get; set; }

    public void RequestServerAuthCode(Action<string> onCode) => onCode?.Invoke(Code);
}

public class BootFakeBackendAuth : IBackendAuthService
{
    public bool IsSignedIn { get; private set; }
    public string PlayerId => IsSignedIn ? "ugs-player" : "";
    public bool SignInCalled { get; private set; }
    public string CodeUsed { get; private set; }
    public bool FailSignIn { get; set; }
    public bool NeverFinishSignIn { get; set; }

    public Task InitializeAsync(string environment) => Task.CompletedTask;

    public Task SignInAsync(string playGamesAuthCode)
    {
        SignInCalled = true;
        CodeUsed = playGamesAuthCode;
        if (NeverFinishSignIn) return new TaskCompletionSource<bool>().Task;
        if (FailSignIn) return Task.FromException(new Exception("sign-in failed"));

        IsSignedIn = true;
        return Task.CompletedTask;
    }

    public Task DeleteAccountAsync()
    {
        IsSignedIn = false;
        return Task.CompletedTask;
    }
}

public class BootFakeCloudStore : ICloudSaveStore
{
    public string LastSaved { get; private set; }

    public Task<string> LoadAsync(string key) => Task.FromResult<string>(null);

    public Task SaveAsync(string key, string value)
    {
        LastSaved = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key) => Task.CompletedTask;
}

public class BootFakeProfiles : IProfileRepository
{
    public UserProfile Profile { get; private set; } = new UserProfile();
    public event Action ProfileSaved;

    public void ReplaceProfile(UserProfile profile)
    {
        Profile = profile;
        ProfileSaved?.Invoke();
    }

    public void SetOwner(string playerId)
    {
        Profile.UserId = playerId;
        ProfileSaved?.Invoke();
    }
}
