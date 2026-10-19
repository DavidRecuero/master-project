using System;
using System.Collections.Generic;
using UnityEngine;

public enum PipeSide { Up, Down, Left, Right }

public enum PipePieceType
{
    StraightVertical,
    StraightHorizontal,
    CornerUpRight,
    CornerUpLeft,
    CornerDownRight,
    CornerDownLeft
}

/// <summary>One tube piece: which sprite goes on which cell.</summary>
public readonly struct PipePiece
{
    public readonly Vector2Int Cell;
    public readonly PipePieceType Type;

    public PipePiece(Vector2Int cell, PipePieceType type)
    {
        Cell = cell;
        Type = type;
    }
}

/// <summary>
/// A mouth (rim) sitting on the edge shared by two cells.
/// Position is in grid space: (A + B) / 2, so it can be turned into world space with the board's grid.
/// </summary>
public readonly struct PipeMouth
{
    public readonly Vector2Int Inside;    // Cell of the tube
    public readonly Vector2Int Outside;   // Cell beyond the edge (virtual inlet cell, or the exit platform)
    public readonly bool IsHorizontalBar; // Vertical flow -> horizontal bar

    public PipeMouth(Vector2Int inside, Vector2Int outside)
    {
        Inside = inside;
        Outside = outside;
        IsHorizontalBar = inside.x == outside.x;
    }

    public Vector2 GridPosition => ((Vector2)Inside + (Vector2)Outside) * 0.5f;

    /// <summary>Unit vector pointing out of the tube through this mouth.</summary>
    public Vector2Int OutwardDirection => Outside - Inside;
}

/// <summary>
/// Pure logic that turns a pipe path into sprites. The last cell of the path is the exit platform, so the tube
/// covers path[0 .. n-2]. Both ends are straight: the inlet behaves as if the tube came from the cell before
/// path[0], and the last tube cell connects to the exit, so every cell is either a straight or a corner.
/// </summary>
public static class PipePieceResolver
{
    public static IReadOnlyList<PipePiece> ResolvePieces(IReadOnlyList<Vector2Int> path)
    {
        var pieces = new List<PipePiece>();
        if (path == null || path.Count < 2) return pieces;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int cell = path[i];
            Vector2Int previous = i > 0 ? path[i - 1] : VirtualInletCell(path);
            Vector2Int next = path[i + 1];

            pieces.Add(new PipePiece(cell, PieceFor(SideTowards(cell, previous), SideTowards(cell, next))));
        }
        return pieces;
    }

    /// <summary>The cell the flow comes from before path[0] (outside the tube).</summary>
    public static Vector2Int VirtualInletCell(IReadOnlyList<Vector2Int> path)
    {
        return path[0] - (path[1] - path[0]);
    }

    public static bool HasTube(IReadOnlyList<Vector2Int> path) => path != null && path.Count >= 2;

    public static PipeMouth InletMouth(IReadOnlyList<Vector2Int> path)
    {
        return new PipeMouth(path[0], VirtualInletCell(path));
    }

    public static PipeMouth OutletMouth(IReadOnlyList<Vector2Int> path)
    {
        return new PipeMouth(path[path.Count - 2], path[path.Count - 1]);
    }

    public static PipeSide SideTowards(Vector2Int from, Vector2Int to)
    {
        Vector2Int d = to - from;
        if (d == Vector2Int.up) return PipeSide.Up;
        if (d == Vector2Int.down) return PipeSide.Down;
        if (d == Vector2Int.left) return PipeSide.Left;
        if (d == Vector2Int.right) return PipeSide.Right;
        throw new ArgumentException($"Cells {from} and {to} are not neighbours: a pipe path must move one cell at a time.");
    }

    public static PipePieceType PieceFor(PipeSide a, PipeSide b)
    {
        if (a == b) throw new ArgumentException($"A pipe cell can't enter and leave through the same side ({a}).");

        bool up = a == PipeSide.Up || b == PipeSide.Up;
        bool down = a == PipeSide.Down || b == PipeSide.Down;
        bool left = a == PipeSide.Left || b == PipeSide.Left;
        bool right = a == PipeSide.Right || b == PipeSide.Right;

        if (up && down) return PipePieceType.StraightVertical;
        if (left && right) return PipePieceType.StraightHorizontal;
        if (up && right) return PipePieceType.CornerUpRight;
        if (up && left) return PipePieceType.CornerUpLeft;
        if (down && right) return PipePieceType.CornerDownRight;
        return PipePieceType.CornerDownLeft;
    }
}
