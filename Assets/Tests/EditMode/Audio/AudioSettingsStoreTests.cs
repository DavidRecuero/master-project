using System.Globalization;
using System.Threading;
using NUnit.Framework;

public class AudioSettingsStoreTests
{
    private FakeStorageProvider _storage;
    private AudioSettingsStore _store;

    [SetUp]
    public void SetUp()
    {
        _storage = new FakeStorageProvider();
        _store = new AudioSettingsStore(_storage);
    }

    [Test]
    public void Defaults_MusicOnAtFullVolume()
    {
        Assert.IsTrue(_store.MusicEnabled);
        Assert.AreEqual(1f, _store.MusicVolume);
    }

    [Test]
    public void MusicEnabled_IsPersisted()
    {
        _store.MusicEnabled = false;

        Assert.IsFalse(new AudioSettingsStore(_storage).MusicEnabled);
    }

    [Test]
    public void MusicVolume_IsPersistedAndClamped()
    {
        _store.MusicVolume = 0.35f;
        Assert.AreEqual(0.35f, new AudioSettingsStore(_storage).MusicVolume, 0.0001f);

        _store.MusicVolume = 3f;
        Assert.AreEqual(1f, _store.MusicVolume);
    }

    [Test]
    public void MusicVolume_SurvivesCommaDecimalCulture()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("es-ES");
            _store.MusicVolume = 0.5f;

            Assert.AreEqual("0.5", _storage.GetString(AudioSettingsStore.MusicVolumeKey));
            Assert.AreEqual(0.5f, _store.MusicVolume, 0.0001f);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Test]
    public void CorruptValue_FallsBackToDefault()
    {
        _storage.SetString(AudioSettingsStore.MusicVolumeKey, "loud");

        Assert.AreEqual(1f, _store.MusicVolume);
    }
}
