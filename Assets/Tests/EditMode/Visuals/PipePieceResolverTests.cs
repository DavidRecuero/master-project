using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Turning a pipe path into sprites: straight/corner pieces with straight ends, mouths on the edges,
/// platform state, inlet counter and depth tint.
/// </summary>
public class PipePieceResolverTests
{
    private static List<Vector2Int> Path(params (int x, int y)[] cells)
    {
        var list = new List<Vector2Int>();
        foreach (var c in cells) list.Add(new Vector2Int(c.x, c.y));
        return list;
    }

    [Test]
    public void StraightDownPipe_UsesVerticalPieces_AndLeavesTheExitWithoutTube()
    {
        var pieces = PipePieceResolver.ResolvePieces(Path((0, 2), (0, 1), (0, 0)));

        Assert.AreEqual(2, pieces.Count, "The last cell is the exit platform, not a tube.");
        Assert.AreEqual(new Vector2Int(0, 2), pieces[0].Cell);
        Assert.AreEqual(PipePieceType.StraightVertical, pieces[0].Type, "The inlet cell is straight, not a rounded cap.");
        Assert.AreEqual(PipePieceType.StraightVertical, pieces[1].Type, "The last tube cell is straight towards the exit.");
    }

    [Test]
    public void Turns_UseTheCornerThatJoinsBothSides()
    {
        // Down, then right, then down to the exit
        var pieces = PipePieceResolver.ResolvePieces(Path((0, 2), (0, 1), (1, 1), (1, 0)));

        Assert.AreEqual(3, pieces.Count);
        Assert.AreEqual(PipePieceType.StraightVertical, pieces[0].Type);
        Assert.AreEqual(PipePieceType.CornerUpRight, pieces[1].Type, "(0,1) joins the cell above and the one on the right.");
        Assert.AreEqual(PipePieceType.CornerDownLeft, pieces[2].Type, "(1,1) joins the cell on the left and the exit below.");
    }

    [Test]
    public void HorizontalInlet_IsStraightHorizontal_WithAVerticalMouthBar()
    {
        var path = Path((0, 0), (1, 0), (2, 0));

        var pieces = PipePieceResolver.ResolvePieces(path);
        PipeMouth inlet = PipePieceResolver.InletMouth(path);

        Assert.AreEqual(PipePieceType.StraightHorizontal, pieces[0].Type);
        Assert.IsFalse(inlet.IsHorizontalBar);
        Assert.AreEqual(new Vector2Int(-1, 0), inlet.OutwardDirection);
    }

    [Test]
    public void Mouths_SitOnTheEdges_OfTheInletAndTheExit()
    {
        var path = Path((0, 2), (0, 1), (0, 0));

        PipeMouth inlet = PipePieceResolver.InletMouth(path);
        PipeMouth outlet = PipePieceResolver.OutletMouth(path);

        Assert.AreEqual(new Vector2(0f, 2.5f), inlet.GridPosition, "Between the first cell and the virtual cell above it.");
        Assert.IsTrue(inlet.IsHorizontalBar);
        Assert.AreEqual(Vector2Int.up, inlet.OutwardDirection);

        Assert.AreEqual(new Vector2(0f, 0.5f), outlet.GridPosition, "Between the last tube cell and the exit platform.");
        Assert.AreEqual(new Vector2Int(0, 0), outlet.Outside);
    }

    [Test]
    public void SingleCellPath_HasNoTube()
    {
        var path = Path((3, 0));

        Assert.IsFalse(PipePieceResolver.HasTube(path));
        Assert.AreEqual(0, PipePieceResolver.ResolvePieces(path).Count);
    }

    [Test]
    public void NonAdjacentCells_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => PipePieceResolver.ResolvePieces(Path((0, 3), (0, 1), (0, 0))));
    }

    [Test]
    public void PieceFor_SameSideTwice_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => PipePieceResolver.PieceFor(PipeSide.Up, PipeSide.Up));
    }

    [Test]
    public void WaitingCount_IsQueueMinusItemsOnTheBoard()
    {
        var pipe = new Pipe { path = Path((0, 1), (0, 0)) };
        for (int i = 0; i < 5; i++) pipe.itemsQueue.Add(ItemSpawnData.Normal(0));

        var go1 = new GameObject("A"); var go2 = new GameObject("B");
        pipe.activeItems.Add(go1.AddComponent<Item>());
        pipe.activeItems.Add(go2.AddComponent<Item>());

        Assert.AreEqual(3, PipeView.WaitingCount(pipe));
        Assert.IsTrue(PipeView.IsPlatformActive(pipe));

        UnityEngine.Object.DestroyImmediate(go1);
        UnityEngine.Object.DestroyImmediate(go2);
    }

    [Test]
    public void EmptyPipe_TurnsThePlatformOff_AndHasNothingWaiting()
    {
        var pipe = new Pipe { path = Path((0, 1), (0, 0)) };

        Assert.AreEqual(0, PipeView.WaitingCount(pipe));
        Assert.IsFalse(PipeView.IsPlatformActive(pipe));
    }

    [Test]
    public void DepthTint_DarkensEachLayerBelowTheTop()
    {
        var set = ScriptableObject.CreateInstance<PipeTileSet>();
        set.depthTintStep = 0.1f;

        Assert.AreEqual(Color.white, set.DepthTint(0));
        Assert.AreEqual(0.8f, set.DepthTint(2).r, 1e-4f);
        Assert.AreEqual(1f, set.DepthTint(2).a, "Tint never makes the tube transparent.");

        UnityEngine.Object.DestroyImmediate(set);
    }
}
