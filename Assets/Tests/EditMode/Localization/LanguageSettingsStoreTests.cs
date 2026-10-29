using NUnit.Framework;

public class LanguageSettingsStoreTests
{
    private FakeStorageProvider _storage;
    private LanguageSettingsStore _store;

    [SetUp]
    public void SetUp()
    {
        _storage = new FakeStorageProvider();
        _store = new LanguageSettingsStore(_storage);
    }

    [Test]
    public void NewPlayer_FollowsTheDevice()
    {
        Assert.IsFalse(_store.HasManualLanguage);
        Assert.IsNull(_store.Language);
    }

    [Test]
    public void ChosenLanguage_IsSaved()
    {
        _store.Language = "ca";

        Assert.IsTrue(_store.HasManualLanguage);
        Assert.AreEqual("ca", _storage.GetString(LanguageSettingsStore.LanguageKey));
        Assert.AreEqual("ca", new LanguageSettingsStore(_storage).Language, "Read back in the next session");
    }

    [Test]
    public void Clear_GoesBackToAutomatic()
    {
        _store.Language = "es";

        _store.Clear();

        Assert.IsFalse(_store.HasManualLanguage);
        Assert.IsNull(_store.Language);
    }

    [Test]
    public void WithoutStorage_NothingBreaks()
    {
        var store = new LanguageSettingsStore(null);

        store.Language = "es";

        Assert.IsFalse(store.HasManualLanguage);
    }
}
