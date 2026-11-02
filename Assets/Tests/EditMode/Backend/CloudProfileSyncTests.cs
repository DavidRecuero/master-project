using System.Threading;
using NUnit.Framework;
using UnityEngine;

public class CloudProfileSyncTests
{
    private const string PlayerId = "ugs-player";

    private FakeCloudSaveStore _store;
    private FakeProfileRepository _profiles;
    private CloudProfileSync _sync;
    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        // No Unity context: awaits continue right away, so every test runs start to end in one go
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        _store = new FakeCloudSaveStore();
        _profiles = new FakeProfileRepository();
        _sync = new CloudProfileSync(_store, _profiles);
    }

    [TearDown]
    public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

    private void PutInCloud(UserProfile profile) => _store.Data[CloudProfileSync.ProfileKey] = JsonUtility.ToJson(profile);

    private UserProfile CloudCopy() => CloudProfileSync.Parse(_store.Data[CloudProfileSync.ProfileKey]);

    private ProfileSource Pull() => _sync.PullAsync(PlayerId).GetAwaiter().GetResult();

    // --- Boot download ---

    [Test]
    public void Pull_WithNoCloudCopy_UploadsTheDeviceCopy_AndTakesOwnership()
    {
        _profiles.Profile = new UserProfile { CurrentLevel = 3, Coins = 40 };

        Assert.AreEqual(ProfileSource.Local, Pull());

        Assert.AreEqual(PlayerId, _profiles.Profile.UserId);
        Assert.AreEqual(3, CloudCopy().CurrentLevel);
        Assert.AreEqual(PlayerId, CloudCopy().UserId);
    }

    [Test]
    public void Pull_WhenTheCloudIsAhead_ReplacesTheDeviceCopy()
    {
        _profiles.Profile = new UserProfile { CurrentLevel = 1 };
        PutInCloud(new UserProfile { CurrentLevel = 7, Coins = 999, UserId = "old-id" });

        Assert.AreEqual(ProfileSource.Cloud, Pull());

        Assert.AreEqual(7, _profiles.Profile.CurrentLevel);
        Assert.AreEqual(999, _profiles.Profile.Coins);
        Assert.AreEqual(PlayerId, _profiles.Profile.UserId);
        Assert.AreEqual(0, _store.SaveCount, "Nothing to upload: both copies are already equal");
    }

    [Test]
    public void Pull_WhenTheDeviceIsAhead_UploadsIt()
    {
        _profiles.Profile = new UserProfile { CurrentLevel = 9 };
        PutInCloud(new UserProfile { CurrentLevel = 4 });

        Assert.AreEqual(ProfileSource.Local, Pull());

        Assert.AreEqual(9, CloudCopy().CurrentLevel);
        Assert.AreEqual(0, _profiles.ReplaceCount);
    }

    [Test]
    public void Pull_ThatArrivesAfterTheBootDeadline_DoesNotSwapTheProfileInUse()
    {
        _profiles.Profile = new UserProfile { CurrentLevel = 1 };
        PutInCloud(new UserProfile { CurrentLevel = 7 });
        _sync.BootDeadlinePassed = true;

        Pull();

        Assert.AreEqual(1, _profiles.Profile.CurrentLevel);
        Assert.IsFalse(_sync.CanUpload, "The device copy is behind: uploading it would overwrite the cloud progress");

        _profiles.SimulateGameSave(p => p.Coins = 1);
        Assert.AreEqual(7, CloudCopy().CurrentLevel);
    }

    [Test]
    public void Pull_ThatFails_Throws_AndLeavesUploadsOff()
    {
        _store.FailLoads = true;

        Assert.Catch(() => { Pull(); });
        Assert.IsFalse(_sync.CanUpload);

        _profiles.SimulateGameSave(p => p.CurrentLevel = 2);
        Assert.AreEqual(0, _store.SaveCount, "A device that couldn't read the cloud must never overwrite it");
    }

    [Test]
    public void Pull_WithAnUnreadableCloudCopy_KeepsTheDeviceCopy()
    {
        _profiles.Profile = new UserProfile { CurrentLevel = 2 };
        _store.Data[CloudProfileSync.ProfileKey] = "not json {";

        Assert.AreEqual(ProfileSource.Local, Pull());
        Assert.AreEqual(2, CloudCopy().CurrentLevel);
    }

    // --- Uploads during the game ---

    [Test]
    public void Saves_BeforeThePull_AreNotUploaded()
    {
        _profiles.SimulateGameSave(p => p.CurrentLevel = 5);

        Assert.AreEqual(0, _store.SaveCount);
    }

    [Test]
    public void Saves_AfterThePull_AreUploaded()
    {
        Pull();
        int savesAfterPull = _store.SaveCount;

        _profiles.SimulateGameSave(p => p.CurrentLevel = 5);

        Assert.AreEqual(savesAfterPull + 1, _store.SaveCount);
        Assert.AreEqual(5, CloudCopy().CurrentLevel);
    }

    [Test]
    public void Saves_DuringAnUpload_AreGroupedIntoOneMoreUpload()
    {
        Pull();
        _store.HoldSaves = true;

        _profiles.SimulateGameSave(p => p.Coins = 1);   // starts an upload, held
        _profiles.SimulateGameSave(p => p.Coins = 2);   // waits
        _profiles.SimulateGameSave(p => p.Coins = 3);   // waits
        Assert.AreEqual(1, _store.HeldSaves, "Never more than one request at a time");

        _store.ReleaseOneSave();
        Assert.AreEqual(1, _store.HeldSaves, "One more upload for the saves that arrived meanwhile");

        _store.ReleaseOneSave();
        Assert.AreEqual(0, _store.HeldSaves);
        Assert.AreEqual(3, CloudCopy().Coins, "The last upload carries the latest state");
    }

    [Test]
    public void FailedUpload_DoesNotThrow_AndTheNextSaveRetries()
    {
        Pull();
        _store.FailSaves = true;

        Assert.DoesNotThrow(() => _profiles.SimulateGameSave(p => p.CurrentLevel = 6));
        Assert.IsTrue(_sync.HasPendingChanges);

        _store.FailSaves = false;
        _profiles.SimulateGameSave(p => p.Coins = 10);

        Assert.IsFalse(_sync.HasPendingChanges);
        Assert.AreEqual(6, CloudCopy().CurrentLevel);
        Assert.AreEqual(10, CloudCopy().Coins);
    }
}
