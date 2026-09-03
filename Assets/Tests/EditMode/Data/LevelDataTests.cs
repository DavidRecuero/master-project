using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LevelDataTests
{
    [Test]
    public void LevelData_CanBeSerializedAndDeserialized()
    {
        // Arrange
        LevelData level = new LevelData
        {
            levelNumber = 3,
            width = 10,
            height = 8,
            pipes = new List<PipeData>
            {
                new PipeData
                {
                    layer = 1,
                    path = new List<Vector2IntData>
                    {
                        new Vector2IntData { x = 0, y = 0 },
                        new Vector2IntData { x = 1, y = 0 },
                        new Vector2IntData { x = 1, y = 1 }
                    },
                    itemsQueue = new List<int> { 1, 2, 3 }
                }
            }
        };

        // Act
        string json = JsonUtility.ToJson(level);
        LevelData loadedLevel = JsonUtility.FromJson<LevelData>(json);

        // Assert
        Assert.AreEqual(level.levelNumber, loadedLevel.levelNumber);
        Assert.AreEqual(level.width, loadedLevel.width);
        Assert.AreEqual(level.height, loadedLevel.height);

        Assert.IsNotNull(loadedLevel.pipes);
        Assert.AreEqual(1, loadedLevel.pipes.Count);

        Assert.AreEqual(1, loadedLevel.pipes[0].layer);

        Assert.IsNotNull(loadedLevel.pipes[0].path);
        Assert.AreEqual(3, loadedLevel.pipes[0].path.Count);
        Assert.AreEqual(1, loadedLevel.pipes[0].path[1].x);
        Assert.AreEqual(1, loadedLevel.pipes[0].path[2].y);

        Assert.IsNotNull(loadedLevel.pipes[0].itemsQueue);
        Assert.AreEqual(3, loadedLevel.pipes[0].itemsQueue.Count);
        Assert.AreEqual(2, loadedLevel.pipes[0].itemsQueue[1]);
    }
}
