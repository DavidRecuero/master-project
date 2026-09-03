using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelLoaderTests
{
    [Test]
    public void LoadLevel_ExistingLevel_ReturnsLevelData()
    {
        // Act
        LevelData level = LevelLoader.LoadLevel(1);

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
        LevelData level = LevelLoader.LoadLevel(999);

        // Assert
        Assert.IsNull(level);
    }
}