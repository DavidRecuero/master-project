using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Offline-first remote settings: the device copy at start, fresh values from the server, and values that never
/// change once the game uses them (a late answer is only saved for the next launch).
/// </summary>
public class RemoteConfigServiceTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    private FakeStorageProvider _storage;
    private FakeRemoteConfigBackend _backend;
    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        // No Unity context: awaits continue right away, so .Result never waits for a frame
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        _storage = new FakeStorageProvider();
        _backend = new FakeRemoteConfigBackend();
    }

    [TearDown]
    public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previousContext);

    private RemoteConfigService Create() => new RemoteConfigService(_backend, new RemoteConfigCache(_storage), () => Now);

    [Test]
    public void FirstLaunch_StartsWithNoValues()
    {
        RemoteConfigService service = Create();

        Assert.AreEqual(RemoteConfigSource.Defaults, service.Source);
        Assert.AreEqual(0, service.Values.Count);
    }

    [Test]
    public void Fetch_UsesAndSavesTheFreshValues()
    {
        _backend.Answer = RemoteTestValues.Of("lives_max", "3");
        RemoteConfigService service = Create();

        Assert.IsTrue(service.FetchAsync().Result);

        Assert.AreEqual(RemoteConfigSource.Remote, service.Source);
        Assert.AreEqual(3, service.Values.GetInt("lives_max", 0));
        Assert.AreEqual(3, new RemoteConfigCache(_storage).Load().GetInt("lives_max", 0));
        Assert.AreEqual(Now, new RemoteConfigCache(_storage).FetchedAtUtc);
    }

    [Test]
    public void NextLaunchOffline_UsesTheSavedValues()
    {
        _backend.Answer = RemoteTestValues.Of("lives_max", "3");
        Create().FetchAsync().Wait();

        _backend.Fail = true;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("last downloaded"));
        RemoteConfigService nextLaunch = Create();
        Assert.IsFalse(nextLaunch.FetchAsync().Result);

        Assert.AreEqual(RemoteConfigSource.Cached, nextLaunch.Source);
        Assert.AreEqual(3, nextLaunch.Values.GetInt("lives_max", 0));
    }

    [Test]
    public void FailedFetch_NeverThrows_AndKeepsTheValues()
    {
        _backend.Fail = true;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("local"));
        RemoteConfigService service = Create();

        Assert.IsFalse(service.FetchAsync().Result);
        Assert.AreEqual(RemoteConfigSource.Defaults, service.Source);
    }

    [Test]
    public void LateAnswer_OnceInUse_IsOnlySavedForTheNextLaunch()
    {
        _backend.HoldNextAnswer();
        RemoteConfigService service = Create();
        var fetch = service.FetchAsync();

        // The Boot gave up waiting: the game starts with what it had
        service.ValuesInUse = true;
        _backend.Release(RemoteTestValues.Of("lives_max", "3"));
        Assert.IsTrue(fetch.Result);

        Assert.AreEqual(RemoteConfigSource.Defaults, service.Source);
        Assert.AreEqual(0, service.Values.Count, "Numbers must not change in the middle of a session");
        Assert.AreEqual(3, Create().Values.GetInt("lives_max", 0), "The next launch starts with them");
    }

    [Test]
    public void NoBackend_NeverFetches()
    {
        var service = new RemoteConfigService(null, null);

        Assert.IsFalse(service.FetchAsync().Result);
        Assert.AreEqual(0, service.Values.Count);
    }

    [Test]
    public void SharedService_HasNoValuesUntilTheBootSetsOne()
    {
        RemoteConfigServices.Set(null);
        Assert.AreEqual(0, RemoteConfigServices.Current.Values.Count);

        var service = new RemoteConfigService(null, null);
        RemoteConfigServices.Set(service);
        Assert.AreSame(service, RemoteConfigServices.Current);

        RemoteConfigServices.Set(null);
    }
}
