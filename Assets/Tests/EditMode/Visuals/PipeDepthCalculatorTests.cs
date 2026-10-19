using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Visual depth comes from real overlaps, not from the layer number.</summary>
public class PipeDepthCalculatorTests
{
    private static Pipe MakePipe(int layer, params (int x, int y)[] cells)
    {
        var path = new List<Vector2Int>();
        foreach (var c in cells) path.Add(new Vector2Int(c.x, c.y));
        return new Pipe { layer = layer, path = path };
    }

    [Test]
    public void PipesThatNobodyCovers_AreNotDarkened_EvenOnLowLayers()
    {
        Pipe low = MakePipe(0, (0, 1), (0, 0));
        Pipe mid = MakePipe(1, (1, 1), (1, 0));
        Pipe top = MakePipe(2, (2, 1), (2, 0));

        var depths = PipeDepthCalculator.Compute(new List<Pipe> { low, mid, top });

        Assert.AreEqual(0, depths[low]);
        Assert.AreEqual(0, depths[mid]);
        Assert.AreEqual(0, depths[top]);
    }

    [Test]
    public void CoveredPipe_IsOneStepBelow_ThePipeThatCoversIt()
    {
        Pipe under = MakePipe(0, (0, 0), (1, 0), (2, 0));
        Pipe over = MakePipe(3, (1, 1), (1, 0), (1, -1));

        var depths = PipeDepthCalculator.Compute(new List<Pipe> { under, over });

        Assert.AreEqual(0, depths[over]);
        Assert.AreEqual(1, depths[under], "Only one pipe on top of it, whatever the layer gap.");
    }

    [Test]
    public void ChainedCrossings_StepDownOneLevelEach()
    {
        Pipe a = MakePipe(2, (0, 0), (1, 0));          // top
        Pipe b = MakePipe(1, (1, 0), (2, 0));          // under a
        Pipe c = MakePipe(0, (2, 0), (3, 0));          // under b only

        var depths = PipeDepthCalculator.Compute(new List<Pipe> { c, b, a });

        Assert.AreEqual(0, depths[a]);
        Assert.AreEqual(1, depths[b]);
        Assert.AreEqual(2, depths[c]);
    }
}
