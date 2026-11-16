using System;
using NUnit.Framework;

/// <summary>The device copy of the last remote settings.</summary>
public class RemoteConfigCacheTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Empty_HasNoValuesAndNoDate()
    {
        var cache = new RemoteConfigCache(new FakeStorageProvider());

        Assert.AreEqual(0, cache.Load().Count);
        Assert.IsNull(cache.FetchedAtUtc);
    }

    [Test]
    public void Save_IsReadBack_IncludingJsonValues()
    {
        var storage = new FakeStorageProvider();
        new RemoteConfigCache(storage).Save(RemoteTestValues.Of("a", "1", "event", "{\"name\":\"xmas\"}"), Now);

        var cache = new RemoteConfigCache(storage);
        RemoteValues values = cache.Load();

        Assert.AreEqual(2, values.Count);
        Assert.AreEqual(1, values.GetInt("a", 0));
        Assert.AreEqual("{\"name\":\"xmas\"}", values.GetString("event", ""));
        Assert.AreEqual(Now, cache.FetchedAtUtc);
    }

    [Test]
    public void BrokenData_IsIgnored()
    {
        var storage = new FakeStorageProvider();
        storage.SetString(RemoteConfigCache.ValuesKey, "{not json");
        storage.SetString(RemoteConfigCache.FetchedAtKey, "yesterday");

        var cache = new RemoteConfigCache(storage);
        RemoteValues values = cache.Load();

        Assert.AreEqual(0, values.Count);
        Assert.IsNull(cache.FetchedAtUtc);
    }

    [Test]
    public void Clear_ForgetsEverything()
    {
        var storage = new FakeStorageProvider();
        var cache = new RemoteConfigCache(storage);
        cache.Save(RemoteTestValues.Of("a", "1"), Now);

        cache.Clear();

        Assert.AreEqual(0, cache.Load().Count);
        Assert.IsNull(cache.FetchedAtUtc);
    }
}
