using NUnit.Framework;

public class HapticsSettingsStoreTests
{
    [Test]
    public void Default_IsOn()
    {
        Assert.IsTrue(new HapticsSettingsStore(new FakeStorageProvider()).Enabled);
    }

    [Test]
    public void Off_IsPersisted()
    {
        var storage = new FakeStorageProvider();
        new HapticsSettingsStore(storage).Enabled = false;

        Assert.IsFalse(new HapticsSettingsStore(storage).Enabled);
    }

    [Test]
    public void BackOn_IsPersisted()
    {
        var storage = new FakeStorageProvider();
        var store = new HapticsSettingsStore(storage) { Enabled = false };
        store.Enabled = true;

        Assert.IsTrue(new HapticsSettingsStore(storage).Enabled);
    }

    [Test]
    public void DoesNotShareItsKeyWithAudio()
    {
        var storage = new FakeStorageProvider();
        new AudioSettingsStore(storage).SfxEnabled = false;

        Assert.IsTrue(new HapticsSettingsStore(storage).Enabled);
    }

    [Test]
    public void NoStorage_IsOnAndDoesNotThrow()
    {
        var store = new HapticsSettingsStore(null);
        Assert.DoesNotThrow(() => store.Enabled = false);
        Assert.IsTrue(store.Enabled);
    }
}
