using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure movement rules of the fog (no Unity objects involved, so it's easy to test).
/// The fog is a group of connected cells. On every move:
///  - EACH fog cell moves to one of its 4 neighbor cells,
///  - no two fog cells can end up in the same cell,
///  - every new cell must be allowed (inside the board, not a pipe exit...),
///  - the group must stay connected (4-neighborhood),
///  - the group must actually change.
/// Since each cell picks its own direction, the cloud changes shape as it drifts.
/// </summary>
public static class FogMover
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// Returns the new position of every fog cell (same order as 'current': result[i] is where
    /// current[i] moved to), or null if the fog can't move at all.
    /// </summary>
    /// <param name="isAllowed">Whether a fog cell can be placed on that cell.</param>
    /// <param name="windBias">0..1. Chance that a cell follows the shared "wind" direction of
    /// this move instead of a random one. Higher = the cloud drifts more as a whole.</param>
    /// <param name="maxAttempts">Random tries before falling back to moving the cloud as a block.</param>
    public static List<Vector2Int> ComputeNextCells(
        IReadOnlyList<Vector2Int> current,
        Func<Vector2Int, bool> isAllowed,
        System.Random rng,
        float windBias = 0.6f,
        int maxAttempts = 200)
    {
        if (current == null || current.Count == 0) return null;

        HashSet<Vector2Int> currentSet = new HashSet<Vector2Int>(current);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<Vector2Int> candidate = TryRandomMove(current, isAllowed, rng, windBias);
            if (candidate == null) continue;

            HashSet<Vector2Int> candidateSet = new HashSet<Vector2Int>(candidate);
            if (candidateSet.SetEquals(currentSet)) continue; // Cells just swapped places: nothing really moved
            if (!IsConnected(candidateSet)) continue;

            return candidate;
        }

        // Fallback: move the whole cloud one step in the same direction (always stays connected)
        foreach (Vector2Int direction in Shuffled(Directions, rng))
        {
            List<Vector2Int> translated = new List<Vector2Int>(current.Count);
            bool valid = true;

            foreach (Vector2Int cell in current)
            {
                Vector2Int target = cell + direction;
                if (!isAllowed(target)) { valid = false; break; }
                translated.Add(target);
            }

            if (valid) return translated;
        }

        return null; // Boxed in: the fog stays where it is
    }

    // One random assignment: every cell picks an allowed, free neighbor
    private static List<Vector2Int> TryRandomMove(
        IReadOnlyList<Vector2Int> current, Func<Vector2Int, bool> isAllowed, System.Random rng, float windBias)
    {
        Vector2Int wind = Directions[rng.Next(Directions.Length)];

        Vector2Int[] result = new Vector2Int[current.Count];
        HashSet<Vector2Int> taken = new HashSet<Vector2Int>();

        // Random processing order, so no cell always gets first pick
        foreach (int index in Shuffled(Range(current.Count), rng))
        {
            Vector2Int cell = current[index];

            List<Vector2Int> options = new List<Vector2Int>(4);
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int target = cell + direction;
                if (isAllowed(target) && !taken.Contains(target)) options.Add(direction);
            }

            if (options.Count == 0) return null;

            Vector2Int chosen = options.Contains(wind) && rng.NextDouble() < windBias
                ? wind
                : options[rng.Next(options.Count)];

            result[index] = cell + chosen;
            taken.Add(cell + chosen);
        }

        return new List<Vector2Int>(result);
    }

    /// <summary>True if every cell can reach every other one moving up/down/left/right through the set.</summary>
    public static bool IsConnected(ICollection<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0) return true;

        HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(cells);
        Queue<Vector2Int> toVisit = new Queue<Vector2Int>();

        IEnumerator<Vector2Int> first = remaining.GetEnumerator();
        first.MoveNext();
        toVisit.Enqueue(first.Current);
        remaining.Remove(first.Current);

        while (toVisit.Count > 0)
        {
            Vector2Int cell = toVisit.Dequeue();
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;
                if (remaining.Remove(neighbor)) toVisit.Enqueue(neighbor);
            }
        }

        return remaining.Count == 0;
    }

    // ---------- Helpers ----------

    private static List<int> Range(int count)
    {
        List<int> list = new List<int>(count);
        for (int i = 0; i < count; i++) list.Add(i);
        return list;
    }

    private static List<T> Shuffled<T>(IEnumerable<T> source, System.Random rng)
    {
        List<T> list = new List<T>(source);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
