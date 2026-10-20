using UnityEngine;

/// <summary>
/// Single source of truth for the draw order of everything on the board.
/// Each pipe layer gets its own band of LayerStride orders, so a higher layer always covers a lower one,
/// and inside a band: shadow, tube, platform, item, mouth, counter.
/// Things that must cover the whole board (fog, crate debris) use AboveBoard, which grows with the level's layers.
/// Tray items use 100, so levels must stay under 10 layers.
/// </summary>
public static class BoardSortingOrder
{
    public const int LayerStride = 10;

    public const int Panel = -50;

    public const int Shadow = 1;
    public const int Tube = 2;
    public const int Platform = 3;
    public const int Item = 4;
    public const int Mouth = 5;
    public const int Counter = 6;
    public const int CounterText = 7;

    public static int For(int layer, int element) => layer * LayerStride + element;

    /// <summary>First order above every pipe band of a board whose top layer is highestLayer.</summary>
    public static int AboveBoard(int highestLayer) => (Mathf.Max(0, highestLayer) + 1) * LayerStride;

    /// <summary>First order above every pipe band of this level.</summary>
    public static int AboveBoard(LevelData data)
    {
        int highest = 0;
        if (data?.pipes != null)
        {
            foreach (PipeData pipe in data.pipes)
            {
                if (pipe != null && pipe.layer > highest) highest = pipe.layer;
            }
        }
        return AboveBoard(highest);
    }
}
