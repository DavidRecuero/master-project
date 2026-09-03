using NUnit.Framework;

public class UserProfileTests
{
    [Test] //Checks that user starts with 250 coins and at level 1
    public void DefaultProfile_StartValues()
    {
        // Arrange & Act
        UserProfile profile = new UserProfile();

        // Assert
        Assert.AreEqual(1, profile.CurrentLevel);
        Assert.AreEqual(250, profile.Coins);
    }
}