using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The shipped levels: every file in the Levels folder is an Addressables entry the game can find
/// (address "level_NNN", label "level"), the numbering has no gaps, and every level loads.
/// </summary>
public class LevelLoaderTests
{
    private LevelLoader _levelLoader;

    [SetUp]
    public void SetUp()
    {
        LevelLibrary.Reset();
        _levelLoader = new LevelLoader();
    }

    [TearDown]
    public void TearDown() => LevelLibrary.Reset();

    [Test]
    public void LoadLevel_ExistingLevel_ReturnsLevelData()
    {
        LevelData level = _levelLoader.LoadLevel(1);

        Assert.IsNotNull(level);
        Assert.AreEqual(1, level.levelNumber);
    }

    [Test]
    public void LoadLevel_NonExistingLevel_ReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "Cannot find level_999 in the 'level' Addressables label!");

        Assert.IsNull(_levelLoader.LoadLevel(999));
    }

    [Test]
    public void CountLevels_MatchesEveryLevelFileInTheFolder()
    {
        Assert.AreEqual(LevelFileNumbers().Count, _levelLoader.CountLevels());
    }

    [Test]
    public void LevelFiles_AreNumberedWithoutGaps()
    {
        // A gap (e.g. level_010 without level_009) would hide every level after it from the game
        List<int> numbers = LevelFileNumbers();

        CollectionAssert.AreEqual(Enumerable.Range(1, numbers.Count), numbers);
    }

    [Test]
    public void CountLevels_EveryCountedLevelLoads()
    {
        int count = _levelLoader.CountLevels();

        Assert.Greater(count, 0);
        for (int level = 1; level <= count; level++)
        {
            Assert.IsNotNull(_levelLoader.LoadLevel(level), $"Level {level}");
        }
    }

    [Test]
    public void EveryLevelFile_IsAnAddressablesEntry_WithItsAddressAndLabel()
    {
        // The game only sees what Addressables lists under the "level" label: a file that isn't an entry
        // works in the editor tools but doesn't exist in a build
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        Assert.IsNotNull(settings, "No Addressables settings in the project");

        foreach (string path in LevelFilePaths())
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            Assert.IsNotNull(entry, $"{name} is not in Addressables (Levels group)");
            Assert.AreEqual(name, entry.address, $"{name} has another address");
            CollectionAssert.Contains(entry.labels, LevelLibrary.Label, $"{name} has no '{LevelLibrary.Label}' label");
        }
    }

    [Test]
    public void NoLevelIsLeftInResources()
    {
        // Resources ships everything inside the build, outside Addressables: a leftover copy would be dead weight
        string[] leftovers = AssetDatabase.FindAssets("level_ t:TextAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Contains("/Resources/"))
            .ToArray();

        CollectionAssert.IsEmpty(leftovers);
    }

    private static List<string> LevelFilePaths() =>
        AssetDatabase.FindAssets("t:TextAsset", new[] { LevelLibrary.Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => LevelCatalog.TryParseNumber(path, out _))
            .ToList();

    private static List<int> LevelFileNumbers() =>
        LevelFilePaths()
            .Select(path => LevelCatalog.TryParseNumber(path, out int number) ? number : 0)
            .OrderBy(number => number)
            .ToList();
}
