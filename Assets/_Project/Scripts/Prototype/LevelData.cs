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
    public List<int> itemsQueue;
}

[System.Serializable]
public class Vector2IntData
{
    public int x;
    public int y;
}