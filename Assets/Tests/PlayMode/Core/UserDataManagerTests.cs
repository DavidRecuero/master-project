using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class UserDataManagerTests
{
    private GameObject _gameObject;
    private UserDataManager _dataManager;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        _gameObject = new GameObject("UserDataManagerTest");
        _dataManager = _gameObject.AddComponent<UserDataManager>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();

        if (_gameObject != null)
        {
            Object.DestroyImmediate(_gameObject);
        }
    }

    [UnityTest]
    public IEnumerator Initialize_LoadsDefaultProfile()
    {
        // Wait for Unity lifecycle methods to execute
        yield return null;

        Assert.IsNotNull(_dataManager.Profile);
        Assert.AreEqual(1, _dataManager.Profile.CurrentLevel);
        Assert.AreEqual(250, _dataManager.Profile.Coins);
    }

    [UnityTest]
    public IEnumerator ResetData_CreatesDefaultProfile()
    {
        // Wait for Unity lifecycle methods to execute
        yield return null;

        // Arrange
        Assert.IsNotNull(_dataManager.Profile);

        _dataManager.Profile.CurrentLevel = 5;
        _dataManager.Profile.Coins = 1000;

        // Act
        _dataManager.ResetData();

        // Assert
        Assert.AreEqual(1, _dataManager.Profile.CurrentLevel);
        Assert.AreEqual(250, _dataManager.Profile.Coins);
    }

    [UnityTest]
    public IEnumerator SetUserId_FillsItIn_WhenEmpty()
    {
        yield return null;

        Assert.IsTrue(string.IsNullOrEmpty(_dataManager.Profile.UserId));

        _dataManager.SetUserId("guest-abc-123");

        Assert.AreEqual("guest-abc-123", _dataManager.Profile.UserId);
    }

    [UnityTest]
    public IEnumerator SetUserId_NeverOverwrites_AnExistingId()
    {
        yield return null;

        _dataManager.SetUserId("first-id");
        _dataManager.SetUserId("second-id");

        Assert.AreEqual("first-id", _dataManager.Profile.UserId, "An existing UserId must never be overwritten by a later call.");
    }


    // --- Cloud sync support (IProfileRepository) ---

    [UnityTest]
    public IEnumerator Saves_StampTheTime_AndRaiseProfileSaved()
    {
        yield return null;

        int saves = 0;
        _dataManager.ProfileSaved += () => saves++;
        long before = System.DateTime.UtcNow.Ticks;

        _dataManager.TrySpendCoins(10);

        Assert.AreEqual(1, saves);
        Assert.GreaterOrEqual(_dataManager.Profile.UpdatedAtTicks, before);
    }

    [UnityTest]
    public IEnumerator ReplaceProfile_TakesTheCloudCopy_KeepingItsTimestamp_AndPersistsIt()
    {
        yield return null;

        var cloud = new UserProfile { CurrentLevel = 8, Coins = 500, UserId = "ugs-1", UpdatedAtTicks = 12345 };

        _dataManager.ReplaceProfile(cloud);

        Assert.AreEqual(8, _dataManager.CurrentLevel);
        Assert.AreEqual(12345, _dataManager.Profile.UpdatedAtTicks);

        UserProfile onDisk = new LocalJsonUserDataService().LoadProfile();
        Assert.AreEqual(8, onDisk.CurrentLevel);
        Assert.AreEqual("ugs-1", onDisk.UserId);
    }

    [UnityTest]
    public IEnumerator SetOwner_ReplacesTheId_EvenIfOneExists()
    {
        yield return null;

        _dataManager.SetUserId("play-games-id");
        _dataManager.SetOwner("ugs-1");

        Assert.AreEqual("ugs-1", _dataManager.UserId, "The backend account owns the profile once the cloud sync runs");
    }

    // --- Level rewards ---

    [UnityTest]
    public IEnumerator WinningALevel_GivesTheCoinsAndSavesThem()
    {
        yield return null;

        int rewarded = -1;
        System.Action<int> listener = coins => rewarded = coins;
        GameEvents.OnLevelRewarded += listener;

        GameEvents.TriggerLevelCleared();
        GameEvents.OnLevelRewarded -= listener;

        Assert.AreEqual(2, _dataManager.CurrentLevel);
        Assert.AreEqual(250 + RewardsConfig.DefaultCoinsPerWin, _dataManager.Coins, "No config on this test object: the default");
        Assert.AreEqual(RewardsConfig.DefaultCoinsPerWin, rewarded);

        UserProfile onDisk = new LocalJsonUserDataService().LoadProfile();
        Assert.AreEqual(250 + RewardsConfig.DefaultCoinsPerWin, onDisk.Coins);
    }
}
