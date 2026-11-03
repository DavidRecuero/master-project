using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>The cloud connection for the whole session: first connection, retries, and its limits.</summary>
public class CloudSessionTests
{
    private FakeBackendAuthService _backend;
    private FakeCloudSaveStore _store;
    private FakeProfileRepository _profiles;
    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        _backend = new FakeBackendAuthService { AccountId = "ugs-1" };
        _store = new FakeCloudSaveStore();
        _profiles = new FakeProfileRepository { Profile = new UserProfile { CurrentLevel = 3 } };
    }

    [TearDown]
    public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

    private DateTime _now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private CloudSession Session(IAuthProvider platform) =>
        new CloudSession(platform, _backend, _store, _profiles, "development", codeTimeoutSeconds: 0.1f, clock: () => _now);

    private static bool Connect(CloudSession session) => session.ConnectAsync().GetAwaiter().GetResult();

    [Test]
    public void Guest_ConnectsAnonymously_AndSyncs()
    {
        CloudSession session = Session(new FakeAuthProvider { IsSignedIn = false });

        Assert.IsTrue(Connect(session));

        Assert.IsNull(_backend.CodeUsed);
        Assert.IsTrue(session.IsConnected);
        Assert.IsTrue(session.Sync.CanUpload);
        Assert.AreEqual("ugs-1", _profiles.Profile.UserId);
    }

    [Test]
    public void PlayGamesPlayer_ConnectsWithTheServerCode()
    {
        CloudSession session = Session(new FakeCodeAuthProvider { IsSignedIn = true, Code = "code-1" });

        Assert.IsTrue(Connect(session));
        Assert.AreEqual("code-1", _backend.CodeUsed);
    }

    [Test]
    public void PlayGamesPlayer_WithoutCode_DoesNotConnect_NeverAsGuest()
    {
        CloudSession session = Session(new FakeCodeAuthProvider { IsSignedIn = true, Code = null });

        Assert.IsFalse(Connect(session));
        Assert.IsFalse(_backend.SignInCalled);
    }

    [Test]
    public void StartedOffline_ConnectsOnALaterTry()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true, Code = null };   // offline: no code
        CloudSession session = Session(platform);
        Assert.IsFalse(Connect(session));

        platform.Code = "code-2";   // network is back

        Assert.IsTrue(Connect(session));
        Assert.AreEqual("code-2", _backend.CodeUsed);
        Assert.IsTrue(session.Sync.CanUpload, "The progress made offline is uploaded from now on");
    }

    [Test]
    public void OnceConnected_FurtherCallsDoNothing()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true };
        CloudSession session = Session(platform);
        Connect(session);

        Connect(session);
        Connect(session);

        Assert.AreEqual(1, _backend.SignInCount);
        Assert.AreEqual(1, platform.CodeRequests, "No new Play Games code each time the game comes back");
    }

    [Test]
    public void CallsDuringAnAttempt_JoinTheSameAttempt()
    {
        _backend.HoldSignIn = new TaskCompletionSource<bool>();
        CloudSession session = Session(new FakeAuthProvider { IsSignedIn = false });

        Task<bool> first = session.ConnectAsync();
        Task<bool> second = session.ConnectAsync();

        Assert.AreSame(first, second);
        Assert.AreEqual(1, _backend.SignInCount);

        _backend.HoldSignIn.SetResult(true);
        Assert.IsTrue(first.GetAwaiter().GetResult());
    }

    [Test]
    public void ReconnectingMidSession_WithTheCloudAhead_DoesNotSwapTheProfileInUse()
    {
        _store.Data[CloudProfileSync.ProfileKey] = JsonUtility.ToJson(new UserProfile { CurrentLevel = 9 });
        CloudSession session = Session(new FakeAuthProvider { IsSignedIn = false });
        session.Sync.ProfileInUse = true;   // the menu is open

        Assert.IsTrue(Connect(session));

        Assert.AreEqual(3, _profiles.Profile.CurrentLevel);
        Assert.IsFalse(session.Sync.CanUpload, "The device copy is behind: it must not overwrite the cloud");
        Assert.AreEqual(9, CloudProfileSync.Parse(_store.Data[CloudProfileSync.ProfileKey]).CurrentLevel);
    }

    [Test]
    public void AfterTheAccountIsDeleted_ItDoesNotConnectAgain()
    {
        CloudSession session = Session(new FakeAuthProvider { IsSignedIn = false });
        Connect(session);
        session.Sync.Stop();                                   // what the account deletion does
        _backend.DeleteAccountAsync().GetAwaiter().GetResult();

        Assert.IsFalse(Connect(session));
        Assert.AreEqual(1, _backend.SignInCount, "No new account until the Boot starts a new session");
    }

    [Test]
    public void Flush_UploadsOnlyWhenAnUploadFailedBefore()
    {
        CloudSession session = Session(new FakeAuthProvider { IsSignedIn = false });
        Connect(session);
        int saves = _store.SaveCount;

        session.FlushAsync().GetAwaiter().GetResult();
        Assert.AreEqual(saves, _store.SaveCount, "Nothing pending, nothing uploaded");

        _store.FailSaves = true;
        _profiles.SimulateGameSave(p => p.Coins = 5);   // fails, stays pending
        _store.FailSaves = false;

        session.FlushAsync().GetAwaiter().GetResult();
        Assert.IsFalse(session.Sync.HasPendingChanges);
        Assert.AreEqual(5, CloudProfileSync.Parse(_store.Data[CloudProfileSync.ProfileKey]).Coins);
    }

    // --- Saves while offline (exponential backoff) ---

    [Test]
    public void SaveWhileOffline_TriesToConnect_RightAway()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true, Code = null };
        CloudSession session = Session(platform);
        Connect(session);                       // the Boot, offline
        platform.Code = "code-back";            // network is back, the game never left the foreground

        _profiles.SimulateGameSave(p => p.CurrentLevel = 4);

        Assert.IsTrue(session.IsSynced);
        Assert.AreEqual(4, CloudProfileSync.Parse(_store.Data[CloudProfileSync.ProfileKey]).CurrentLevel);
    }

    [Test]
    public void SavesWhileStillOffline_WaitLongerAndLongerBetweenTries()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true, Code = null };
        CloudSession session = Session(platform);

        _profiles.SimulateGameSave(p => p.Coins = 1);     // try 1, fails
        Assert.AreEqual(1, platform.CodeRequests);

        _profiles.SimulateGameSave(p => p.Coins = 2);     // too soon
        _now = _now.AddSeconds(29);
        _profiles.SimulateGameSave(p => p.Coins = 3);     // still too soon
        Assert.AreEqual(1, platform.CodeRequests);

        _now = _now.AddSeconds(1);                        // 30 s after try 1
        _profiles.SimulateGameSave(p => p.Coins = 4);     // try 2, fails
        Assert.AreEqual(2, platform.CodeRequests);

        _now = _now.AddSeconds(59);
        _profiles.SimulateGameSave(p => p.Coins = 5);     // the wait doubled to 60 s
        Assert.AreEqual(2, platform.CodeRequests);

        _now = _now.AddSeconds(1);
        _profiles.SimulateGameSave(p => p.Coins = 6);     // try 3
        Assert.AreEqual(3, platform.CodeRequests);
    }

    [Test]
    public void TheWaitBetweenTries_NeverGoesOverFiveMinutes()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true, Code = null };
        CloudSession session = Session(platform);

        for (int i = 0; i < 10; i++)
        {
            _profiles.SimulateGameSave(p => p.Coins++);
            _now = session.NextSaveTry;               // jump to when the next try is allowed
        }

        _profiles.SimulateGameSave(p => p.Coins++);
        Assert.LessOrEqual(session.NextSaveTry - _now, CloudSession.MaxBackoff);
    }

    [Test]
    public void SavesWhenAlreadyConnected_DoNotTryAgain()
    {
        var platform = new FakeCodeAuthProvider { IsSignedIn = true };
        CloudSession session = Session(platform);
        Connect(session);

        _profiles.SimulateGameSave(p => p.Coins = 7);
        _profiles.SimulateGameSave(p => p.Coins = 8);

        Assert.AreEqual(1, platform.CodeRequests);
        Assert.AreEqual(1, _backend.SignInCount);
    }
}
