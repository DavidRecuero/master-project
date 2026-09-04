using NUnit.Framework;

public class LocalJsonUserDataServiceTests
{
    private FakeStorageProvider _fakeStorage;
    private LocalJsonUserDataService _service;

    [SetUp]
    public void SetUp()
    {
        _fakeStorage = new FakeStorageProvider();
        _service = new LocalJsonUserDataService(_fakeStorage);
    }

    [Test]
    public void LoadProfile_WhenNoProfileExists_CreatesAndSavesDefaultProfile()
    {
        Assert.IsFalse(_service.HasProfile());

        UserProfile profile = _service.LoadProfile();

        Assert.IsNotNull(profile);
        Assert.AreEqual(1, profile.CurrentLevel);
        Assert.AreEqual(250, profile.Coins);
        Assert.IsTrue(_service.HasProfile());
    }

    [Test]
    public void SaveProfile_PersistsProfileInStorage()
    {
        UserProfile profile = new UserProfile { CurrentLevel = 5, Coins = 1000 };

        _service.SaveProfile(profile);
        UserProfile loadedProfile = _service.LoadProfile();

        Assert.AreEqual(5, loadedProfile.CurrentLevel);
        Assert.AreEqual(1000, loadedProfile.Coins);
    }
}