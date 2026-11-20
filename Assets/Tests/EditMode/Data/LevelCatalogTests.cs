using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The level catalog (addresses → levels, the no-gap rule) and the shared LevelLibrary.</summary>
public class LevelCatalogTests
{
    private static KeyValuePair<string, string> Level(string key, string json = "{}") =>
        new KeyValuePair<string, string>(key, json);

    // ---------- LevelCatalog ----------

    [TestCase("level_001", 1)]
    [TestCase("level_038", 38)]
    [TestCase("level_120", 120)]
    [TestCase("Assets/_Project/Levels/level_007.json", 7)]
    [TestCase("level_009.json", 9)]
    public void Address_IsParsed(string key, int expected)
    {
        Assert.IsTrue(LevelCatalog.TryParseNumber(key, out int number));
        Assert.AreEqual(expected, number);
    }

    [TestCase("level_000")]
    [TestCase("level_abc")]
    [TestCase("mylevel_001")]
    [TestCase("level_001_old")]
    [TestCase("")]
    [TestCase(null)]
    public void NotALevelAddress_IsIgnored(string key)
    {
        Assert.IsFalse(LevelCatalog.TryParseNumber(key, out _));
    }

    [Test]
    public void Address_IsThreeDigits()
    {
        Assert.AreEqual("level_001", LevelCatalog.Address(1));
        Assert.AreEqual("level_120", LevelCatalog.Address(120));
    }

    [Test]
    public void Count_StopsAtTheFirstGap()
    {
        var catalog = new LevelCatalog(new[] { Level("level_001"), Level("level_002"), Level("level_004") });

        Assert.AreEqual(2, catalog.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 4 }, catalog.Numbers);
    }

    [Test]
    public void Catalog_KeepsTheJson_AndSkipsEmptyFiles()
    {
        var catalog = new LevelCatalog(new[] { Level("level_001", "{\"levelNumber\":1}"), Level("level_002", "") });

        Assert.IsTrue(catalog.TryGetJson(1, out string json));
        Assert.AreEqual("{\"levelNumber\":1}", json);
        Assert.IsFalse(catalog.TryGetJson(2, out _));
        Assert.AreEqual(1, catalog.Count);
    }

    [Test]
    public void Empty_HasNoLevels()
    {
        Assert.AreEqual(0, LevelCatalog.Empty.Count);
        Assert.IsFalse(LevelCatalog.Empty.TryGetJson(1, out _));
    }

    // ---------- LevelLibrary ----------

    private SynchronizationContext _previousContext;

    [SetUp]
    public void SetUp()
    {
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
    }

    [TearDown]
    public void TearDown()
    {
        LevelLibrary.Reset();
        SynchronizationContext.SetSynchronizationContext(_previousContext);
    }

    [Test]
    public void LoadAsync_SharesTheCatalog()
    {
        var source = new FakeLevelSource { Catalog = new LevelCatalog(new[] { Level("level_001"), Level("level_002") }) };
        LevelLibrary.SetSource(source);

        LevelLibrary.LoadAsync().Wait();

        Assert.IsTrue(LevelLibrary.IsLoaded);
        Assert.AreEqual(2, new LevelLoader().CountLevels());
        Assert.AreEqual(0, source.LoadNowCalls, "Already loaded: no second load");
    }

    [Test]
    public void LoadAsync_Failure_NeverThrows_AndStaysUnloaded()
    {
        LevelLibrary.SetSource(new FakeLevelSource { Fail = true });
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("retrying"));

        Assert.DoesNotThrow(() => LevelLibrary.LoadAsync().Wait());
        Assert.IsFalse(LevelLibrary.IsLoaded);
    }

    private class FakeLevelSource : ILevelSource
    {
        public LevelCatalog Catalog = LevelCatalog.Empty;
        public bool Fail;
        public int LoadNowCalls;

        public Task<LevelCatalog> LoadAsync() =>
            Fail ? Task.FromException<LevelCatalog>(new InvalidOperationException("no content")) : Task.FromResult(Catalog);

        public LevelCatalog LoadNow()
        {
            LoadNowCalls++;
            return Catalog;
        }
    }
}
