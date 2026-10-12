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

    [Test]
    public void BuildSpawnQueue_ConvertsLegacyItemsQueue_IntoNormalItems()
    {
        var pipeData = new PipeData { itemsQueue = new List<int> { 3, 1 } };

        List<ItemSpawnData> queue = pipeData.BuildSpawnQueue();

        Assert.AreEqual(2, queue.Count);
        Assert.AreEqual(3, queue[0].colorId);
        Assert.AreEqual(ItemType.Normal, queue[0].type);
        Assert.AreEqual(1, queue[1].colorId);
    }

    [Test]
    public void BuildSpawnQueue_PrefersTypedItems_OverLegacyQueue()
    {
        var pipeData = new PipeData
        {
            itemsQueue = new List<int> { 0, 0, 0 },
            items = new List<ItemSpawnData>
            {
                new ItemSpawnData { colorId = 2, type = ItemType.Hidden },
                new ItemSpawnData { colorId = 1, type = ItemType.Barrel, lives = 3, contentHidden = true }
            }
        };

        List<ItemSpawnData> queue = pipeData.BuildSpawnQueue();

        Assert.AreEqual(2, queue.Count);
        Assert.AreEqual(ItemType.Hidden, queue[0].type);
        Assert.AreEqual(ItemType.Barrel, queue[1].type);
        Assert.AreEqual(3, queue[1].lives);
        Assert.IsTrue(queue[1].contentHidden);
    }

    [Test]
    public void BuildSpawnQueue_ReturnsCopies_SoRuntimeChangesDontTouchLevelData()
    {
        var original = new ItemSpawnData { colorId = 1 };
        var pipeData = new PipeData { items = new List<ItemSpawnData> { original } };

        List<ItemSpawnData> queue = pipeData.BuildSpawnQueue();
        queue[0].colorId = 3;

        Assert.AreEqual(1, original.colorId);
    }

    [Test]
    public void TypedItems_CanBeParsedFromJson()
    {
        string json = "{\"levelNumber\":6,\"width\":3,\"height\":3,\"pipes\":[{\"layer\":0,\"path\":[{\"x\":0,\"y\":0}]," +
                      "\"items\":[{\"colorId\":2},{\"colorId\":1,\"type\":2,\"lives\":4}]}]}";

        LevelData level = JsonUtility.FromJson<LevelData>(json);
        List<ItemSpawnData> queue = level.pipes[0].BuildSpawnQueue();

        Assert.AreEqual(2, queue.Count);
        Assert.AreEqual(ItemType.Normal, queue[0].type);
        Assert.AreEqual(ItemType.Barrel, queue[1].type);
        Assert.AreEqual(4, queue[1].lives);
    }
}
