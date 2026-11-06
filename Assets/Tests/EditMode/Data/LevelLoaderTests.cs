using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelLoaderTests
{
    private LevelLoader _levelLoader;

    [SetUp]
    public void SetUp()
    {
        _levelLoader = new LevelLoader();
    }

    [Test]
    public void LoadLevel_ExistingLevel_ReturnsLevelData()
    {
        // Act
        LevelData level = _levelLoader.LoadLevel(1);

        // Assert
        Assert.IsNotNull(level);
        Assert.AreEqual(1, level.levelNumber);
    }

    [Test]
    public void LoadLevel_NonExistingLevel_ReturnsNull()
    {
        // Arrange
        LogAssert.Expect(
            LogType.Error,
            "Cannot find level_999.json in the Resources folder!"
        );

        // Act
        LevelData level = _levelLoader.LoadLevel(999);

        // Assert
        Assert.IsNull(level);
    }

    [Test]
    public void CountLevels_MatchesEveryLevelFileInResources()
    {
        List<int> numbers = LevelFileNumbersInResources();

        Assert.AreEqual(numbers.Count, _levelLoader.CountLevels());
    }

    [Test]
    public void LevelFiles_AreNumberedWithoutGaps()
    {
        // A gap (e.g. level_010 without level_009) would hide every level after it from the game
        List<int> numbers = LevelFileNumbersInResources();

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

    private static List<int> LevelFileNumbersInResources()
    {
        Regex levelName = new Regex(@"^level_(\d+)$");

        return AssetDatabase.FindAssets("level_ t:TextAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Contains("/Resources/"))
            .Select(path => levelName.Match(System.IO.Path.GetFileNameWithoutExtension(path)))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .OrderBy(number => number)
            .ToList();
    }
}
