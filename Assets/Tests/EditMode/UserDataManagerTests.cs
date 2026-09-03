using NUnit.Framework;
using UnityEngine.SocialPlatforms.Impl;

public class UserDataManagerTests
{
    private LocalJsonUserDataService _dataService;

    [SetUp]
    public void SetUp()
    {
        _dataService = new LocalJsonUserDataService();
    }

    [Test]  // Tests that the coins are updatable
    public void SaveAndLoadProfile_UpdatesCoinsCorrectly()
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

    [Test] //Checks that user starts with 250 coins and at level 1
    public void DefaultProfile_StartsAtLevelOneAndZeroCoins()
    {
        // Arrange & Act
        UserProfile profile = new UserProfile();

        // Assert
        Assert.AreEqual(1, profile.CurrentLevel);
        Assert.AreEqual(250, profile.Coins);
    }
}