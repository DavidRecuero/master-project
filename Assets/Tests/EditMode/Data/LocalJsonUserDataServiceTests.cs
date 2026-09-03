using NUnit.Framework;
using UnityEngine;

public class LocalJsonUserDataServiceTests
{
    private LocalJsonUserDataService _dataService;

    [SetUp]
    public void SetUp()
    {
        _dataService = new LocalJsonUserDataService();

        // To start without saved data
        PlayerPrefs.DeleteAll();
    }

    [TearDown] // Clean up after each test
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void SaveAndLoadProfile_PreservesProfileData()
    {
        // Arrange
        UserProfile profile = new UserProfile();
        profile.Coins = 500;

        // Act
        _dataService.SaveProfile(profile);
        UserProfile loadedProfile = _dataService.LoadProfile();

        // Assert
        Assert.AreEqual(500, loadedProfile.Coins);
    }

    [Test]
    public void LoadProfile_WhenNoProfileExists_CreatesDefaultProfile()
    {
        // Arrange
        Assert.IsFalse(_dataService.HasProfile());

        // Act
        UserProfile profile = _dataService.LoadProfile();

        // Assert
        Assert.AreEqual(1, profile.CurrentLevel);
        Assert.AreEqual(250, profile.Coins);
        Assert.IsTrue(_dataService.HasProfile());
    }

    [Test]
    public void HasProfile_WhenProfileDoesNotExist_ReturnsFalse()
    {
        // Act
        bool hasProfile = _dataService.HasProfile();

        // Assert
        Assert.IsFalse(hasProfile);
    }

    [Test]
    public void HasProfile_WhenProfileIsSaved_ReturnsTrue()
    {
        // Arrange
        UserProfile profile = new UserProfile();

        // Act
        _dataService.SaveProfile(profile);

        // Assert
        Assert.IsTrue(_dataService.HasProfile());
    }
}
