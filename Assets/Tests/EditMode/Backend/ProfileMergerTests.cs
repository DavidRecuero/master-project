using NUnit.Framework;

public class ProfileMergerTests
{
    private static UserProfile Profile(int level, long updatedAt = 0) =>
        new UserProfile { CurrentLevel = level, UpdatedAtTicks = updatedAt };

    [Test]
    public void NoCloudCopy_DeviceWins()
    {
        Assert.AreEqual(ProfileSource.Local, ProfileMerger.Choose(Profile(3), null));
    }

    [Test]
    public void NoDeviceCopy_CloudWins()
    {
        Assert.AreEqual(ProfileSource.Cloud, ProfileMerger.Choose(null, Profile(3)));
    }

    [Test]
    public void HigherLevel_Wins_EvenIfSavedEarlier()
    {
        Assert.AreEqual(ProfileSource.Cloud, ProfileMerger.Choose(Profile(2, updatedAt: 900), Profile(5, updatedAt: 100)));
        Assert.AreEqual(ProfileSource.Local, ProfileMerger.Choose(Profile(5, updatedAt: 100), Profile(2, updatedAt: 900)));
    }

    [Test]
    public void SameLevel_LastSaveWins()
    {
        Assert.AreEqual(ProfileSource.Cloud, ProfileMerger.Choose(Profile(4, updatedAt: 100), Profile(4, updatedAt: 200)));
        Assert.AreEqual(ProfileSource.Local, ProfileMerger.Choose(Profile(4, updatedAt: 200), Profile(4, updatedAt: 100)));
    }

    [Test]
    public void FullTie_DeviceWins()
    {
        Assert.AreEqual(ProfileSource.Local, ProfileMerger.Choose(Profile(4, updatedAt: 100), Profile(4, updatedAt: 100)));
    }

    [Test]
    public void NewInstall_TakesTheCloudProgress()
    {
        // Fresh profile on a new device: level 1, never saved (0 ticks)
        Assert.AreEqual(ProfileSource.Cloud, ProfileMerger.Choose(new UserProfile(), Profile(1, updatedAt: 50)));
    }
}
