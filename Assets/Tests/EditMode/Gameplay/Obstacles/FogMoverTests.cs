using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class FogMoverTests
{
    // 6x6 board where the bottom row (y = 0) holds the pipe exits, so fog can't go there
    private static bool AllowedOn6x6WithExitRow(Vector2Int c) => c.x >= 0 && c.x < 6 && c.y >= 1 && c.y < 6;

    private static readonly List<Vector2Int> Cloud = new List<Vector2Int>
    {
        new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(3, 2)
    };

    // ---------- IsConnected ----------

    [Test]
    public void IsConnected_True_ForAnLShape()
    {
        var cells = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) };
        Assert.IsTrue(FogMover.IsConnected(cells));
    }

    [Test]
    public void IsConnected_False_ForDiagonalOnlyContact()
    {
        var cells = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1) };
        Assert.IsFalse(FogMover.IsConnected(cells), "Diagonal cells are not neighbors.");
    }

    [Test]
    public void IsConnected_False_ForTwoSeparateGroups()
    {
        var cells = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(4, 0), new Vector2Int(5, 0) };
        Assert.IsFalse(FogMover.IsConnected(cells));
    }

    // ---------- Movement rules (checked over many random moves) ----------

    [Test]
    public void EveryMove_FollowsAllTheRules()
    {
        var rng = new System.Random(1234);
        List<Vector2Int> current = new List<Vector2Int>(Cloud);

        for (int move = 0; move < 500; move++)
        {
            List<Vector2Int> next = FogMover.ComputeNextCells(current, AllowedOn6x6WithExitRow, rng);
            Assert.IsNotNull(next, $"Move {move}: the cloud has room, it should always be able to move.");

            Assert.AreEqual(current.Count, next.Count, "The fog never grows or shrinks.");
            Assert.AreEqual(next.Count, next.Distinct().Count(), "Two fog cells can't share a cell.");
            Assert.IsTrue(FogMover.IsConnected(next), "The cloud must stay connected.");
            Assert.IsTrue(next.All(AllowedOn6x6WithExitRow), "Fog can't enter forbidden cells (exits / outside).");
            Assert.IsFalse(new HashSet<Vector2Int>(next).SetEquals(current), "The cloud must actually move.");

            for (int i = 0; i < current.Count; i++)
            {
                Vector2Int delta = next[i] - current[i];
                Assert.AreEqual(1, Mathf.Abs(delta.x) + Mathf.Abs(delta.y),
                    "Each fog cell moves exactly one step to a neighbor cell.");
            }

            current = next;
        }
    }

    [Test]
    public void TheCloudChangesShape_OverTime()
    {
        var rng = new System.Random(7);
        List<Vector2Int> current = new List<Vector2Int>(Cloud);
        string originalShape = ShapeSignature(current);
        bool shapeChanged = false;

        for (int move = 0; move < 50 && !shapeChanged; move++)
        {
            current = FogMover.ComputeNextCells(current, AllowedOn6x6WithExitRow, rng);
            shapeChanged = ShapeSignature(current) != originalShape;
        }

        Assert.IsTrue(shapeChanged, "Cells move independently, so the cloud shouldn't keep the same shape forever.");
    }

    [Test]
    public void ReturnsNull_WhenTheFogIsBoxedIn()
    {
        // A single fog cell whose 4 neighbors are all forbidden
        var single = new List<Vector2Int> { new Vector2Int(2, 2) };
        bool OnlyItsOwnCell(Vector2Int c) => c == new Vector2Int(2, 2);

        Assert.IsNull(FogMover.ComputeNextCells(single, OnlyItsOwnCell, new System.Random(1)));
    }

    [Test]
    public void SameSeed_GivesTheSameMove()
    {
        List<Vector2Int> a = FogMover.ComputeNextCells(Cloud, AllowedOn6x6WithExitRow, new System.Random(99));
        List<Vector2Int> b = FogMover.ComputeNextCells(Cloud, AllowedOn6x6WithExitRow, new System.Random(99));

        CollectionAssert.AreEqual(a, b);
    }

    // Shape independent of position: cells relative to the bottom-left corner of the group
    private static string ShapeSignature(List<Vector2Int> cells)
    {
        int minX = cells.Min(c => c.x), minY = cells.Min(c => c.y);
        return string.Join(";", cells.Select(c => new Vector2Int(c.x - minX, c.y - minY))
                                     .OrderBy(c => c.x).ThenBy(c => c.y)
                                     .Select(c => $"{c.x},{c.y}"));
    }
}
