using NUnit.Framework;
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
}