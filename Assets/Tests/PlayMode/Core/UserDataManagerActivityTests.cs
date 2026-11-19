using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// What the profile keeps for segmentation: the fail streak (losing and abandoning add one, winning resets it)
/// and the launches (days played, last launch).
/// </summary>
public class UserDataManagerActivityTests
{
    private GameObject _gameObject;
    private UserDataManager _dataManager;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        _gameObject = new GameObject("UserDataManagerActivityTest");
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

    // ---------- Launches ----------

    private static readonly System.DateTime Monday = new System.DateTime(2026, 11, 16, 10, 0, 0, System.DateTimeKind.Utc);

    [UnityTest]
    public IEnumerator FirstLaunch_CountsOneDay()
    {
        yield return null;

        _dataManager.RegisterSession(Monday);

        Assert.AreEqual(1, _dataManager.Profile.DaysPlayed);
        Assert.AreEqual(Monday.Ticks, _dataManager.Profile.LastSessionTicks);
    }

    [UnityTest]
    public IEnumerator SameDay_CountsOnce_NewDay_CountsAgain()
    {
        yield return null;

        _dataManager.RegisterSession(Monday);
        _dataManager.RegisterSession(Monday.AddHours(5));
        Assert.AreEqual(1, _dataManager.Profile.DaysPlayed);

        _dataManager.RegisterSession(Monday.AddDays(3));
        Assert.AreEqual(2, _dataManager.Profile.DaysPlayed);
        Assert.AreEqual(Monday.AddDays(3).Ticks, _dataManager.Profile.LastSessionTicks);
    }
}
