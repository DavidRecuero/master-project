using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public int width;
    public int height;
    public List<PipeData> pipes;
}

[System.Serializable]
public class PipeData
{
    public int layer;
    public List<Vector2IntData> path;

    // Legacy format: plain color ids. Still supported so old levels keep working.
    public List<int> itemsQueue;

    // New format: typed elements (normal, hidden, barrel...). Takes priority over itemsQueue if present.
    public List<ItemSpawnData> items;

    /// <summary>
    /// Builds the runtime spawn queue for this pipe. Uses 'items' when the level defines it,
    /// otherwise converts the legacy 'itemsQueue' into normal items.
    /// Always returns fresh copies, so runtime changes never touch the loaded level data.
    /// </summary>
    public List<ItemSpawnData> BuildSpawnQueue()
    {
        List<ItemSpawnData> queue = new List<ItemSpawnData>();

        if (items != null && items.Count > 0)
        {
            foreach (ItemSpawnData data in items)
            {
                if (data != null) queue.Add(data.Clone());
            }
            return queue;
        }

        if (itemsQueue != null)
        {
            foreach (int colorId in itemsQueue)
            {
                queue.Add(ItemSpawnData.Normal(colorId));
            }
        }

        return queue;
    }
}

[System.Serializable]
public class Vector2IntData
{
    public int x;
    public int y;
}
