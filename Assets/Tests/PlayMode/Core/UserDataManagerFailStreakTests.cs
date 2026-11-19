using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The fail streak kept in the profile for segmentation: losing and abandoning add one, winning resets it.</summary>
public class UserDataManagerFailStreakTests
{
    private GameObject _gameObject;
    private UserDataManager _dataManager;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        _gameObject = new GameObject("UserDataManagerFailStreakTest");
        _dataManager = _gameObject.AddComponent<UserDataManager>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        if (_gameObject != null) Object.DestroyImmediate(_gameObject);
    }

    [UnityTest]
    public IEnumerator LosingAndAbandoning_AddToTheStreak()
    {
        yield return null;

        GameEvents.TriggerLevelFailed();
        GameEvents.TriggerLevelAbandoned();

        Assert.AreEqual(2, _dataManager.Profile.FailStreak);
    }

    [UnityTest]
    public IEnumerator Winning_ResetsTheStreak()
    {
        yield return null;

        GameEvents.TriggerLevelFailed();
        GameEvents.TriggerLevelFailed();
        GameEvents.TriggerLevelCleared();

        Assert.AreEqual(0, _dataManager.Profile.FailStreak);
    }

    [UnityTest]
    public IEnumerator Streak_IsSaved()
    {
        yield return null;

        GameEvents.TriggerLevelFailed();

        UserProfile saved = new LocalJsonUserDataService().LoadProfile();
        Assert.AreEqual(1, saved.FailStreak);
    }
}
