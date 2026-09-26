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

}