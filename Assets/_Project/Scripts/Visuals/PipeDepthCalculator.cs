using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Visual depth of each pipe, based on what actually covers it instead of its layer number.
/// A pipe that shares no cell with a higher-layer pipe is fully visible (depth 0), whatever its layer.
/// Otherwise its depth is 1 + the deepest of the pipes that cover it, so stacked crossings step down gradually.
/// </summary>
public static class PipeDepthCalculator
{
    public static Dictionary<Pipe, int> Compute(IReadOnlyList<Pipe> pipes)
    {
        var depths = new Dictionary<Pipe, int>();
        if (pipes == null) return depths;

        List<Pipe> valid = pipes.Where(p => p != null && p.HasPath).ToList();
        var cells = valid.ToDictionary(p => p, p => new HashSet<Vector2Int>(p.path));

        // Highest layers first: whoever can cover a pipe already has its depth when we reach it
        foreach (Pipe pipe in valid.OrderByDescending(p => p.layer))
        {
            int depth = 0;
            foreach (Pipe other in valid)
            {
                if (other.layer <= pipe.layer || !depths.ContainsKey(other)) continue;
                if (!cells[other].Overlaps(cells[pipe])) continue;

                depth = Mathf.Max(depth, depths[other] + 1);
            }
            depths[pipe] = depth;
        }
        return depths;
    }
}
