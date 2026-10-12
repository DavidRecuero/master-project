using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pipe
{
    [Tooltip("Higher layer value will be drawn on top of lower layer pipes")]
    public int layer;

    [Tooltip("List of positions. Index 0 is the origin, the last index is the destination")]
    public List<Vector2Int> path;

    [Tooltip("Queue of elements to spawn. Index i of this queue always matches activeItems[i] while it is on the board.")]
    public List<ItemSpawnData> itemsQueue = new List<ItemSpawnData>();

    // Runtime list tracking the active GameObjects sitting on the path (index 0 = the one at the exit)
    [HideInInspector]
    public List<Item> activeItems = new List<Item>();

    public bool HasPath => path != null && path.Count > 0;

    public Vector2Int ExitPosition => path[path.Count - 1];

    /// <summary>True when nothing is left on the board nor waiting in the queue.</summary>
    public bool IsEmpty => activeItems.Count == 0 && itemsQueue.Count == 0;

    /// <summary>The element currently sitting at the exit, or null if the pipe is empty.</summary>
    public Item ExitItem => activeItems.Count > 0 ? activeItems[0] : null;
}
