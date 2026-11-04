using System.Collections.Generic;
using NUnit.Framework;

public class BoardSortingOrderTests
{
    private static LevelData LevelWithLayers(params int[] layers)
    {
        var data = new LevelData { pipes = new List<PipeData>() };
        foreach (int layer in layers) data.pipes.Add(new PipeData { layer = layer });
        return data;
    }

    [Test]
    public void AboveBoard_IsAboveEveryElementOfTheTopLayer()
    {
        int top = BoardSortingOrder.For(5, BoardSortingOrder.CounterText);

        Assert.Greater(BoardSortingOrder.AboveBoard(5), top);
    }

    [Test]
    public void AboveBoard_FromLevelData_UsesTheHighestLayer()
    {
        // Level 8 has six layers (0..5): the old fixed fog order (50) sat inside layer 5's band
        LevelData data = LevelWithLayers(0, 3, 5, 1);

        Assert.AreEqual(60, BoardSortingOrder.AboveBoard(data));
        Assert.Greater(BoardSortingOrder.AboveBoard(data), BoardSortingOrder.For(5, BoardSortingOrder.Mouth));
    }

    [Test]
    public void Glass_CoversTheItemsInTheTube_ButNotTheMouths()
    {
        Assert.Greater(BoardSortingOrder.Glass, BoardSortingOrder.Item);
        Assert.Greater(BoardSortingOrder.Glass, BoardSortingOrder.Platform);
        Assert.Less(BoardSortingOrder.Glass, BoardSortingOrder.Mouth);
        Assert.Less(BoardSortingOrder.CounterText, BoardSortingOrder.LayerStride, "Everything of a layer fits in its band");
    }

    [Test]
    public void TileSet_HasAGlassForEveryTubePiece()
    {
        var set = UnityEngine.ScriptableObject.CreateInstance<PipeTileSet>();
        try
        {
            var sprite = UnityEngine.Sprite.Create(new UnityEngine.Texture2D(2, 2), new UnityEngine.Rect(0, 0, 2, 2), UnityEngine.Vector2.zero);
            set.glassStraightVertical = set.glassStraightHorizontal = sprite;
            set.glassCornerUpRight = set.glassCornerUpLeft = set.glassCornerDownRight = set.glassCornerDownLeft = sprite;

            foreach (PipePieceType type in System.Enum.GetValues(typeof(PipePieceType)))
                Assert.AreSame(sprite, set.GetGlass(type), type.ToString());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(set);
        }
    }

    [Test]
    public void AboveBoard_WithoutPipes_IsAboveLayerZero()
    {
        Assert.AreEqual(10, BoardSortingOrder.AboveBoard(new LevelData()));
        Assert.AreEqual(10, BoardSortingOrder.AboveBoard((LevelData)null));
    }
}
