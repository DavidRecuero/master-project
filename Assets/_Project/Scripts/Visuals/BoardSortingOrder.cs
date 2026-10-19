/// <summary>
/// Single source of truth for the draw order of everything on the board.
/// Each pipe layer gets its own band of LayerStride orders, so a higher layer always covers a lower one,
/// and inside a band: shadow, tube, platform, item, mouth, counter.
/// Keep fog and tray items above the highest band (fog uses 50, tray items 100 by default).
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
}
