using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Rules of the barrel obstacle:
///  - A barrel travels through its pipe like any element, but can't be tapped while it has lives.
///  - Once it reaches the exit, every item that leaves the board from a neighbor cell takes one life from it, whether the player tapped it or the Match booster pulled it.
///  - At 0 lives it breaks and leaves the element it was holding (normal or hidden).
///  - If every neighbor pipe is empty, it breaks on its own so the level can never get stuck.
/// Visual data lives in BoardManager; this class only decides when to damage/break.
/// </summary>
public class BarrelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;

    [Header("Rules")]
    [Tooltip("Lives removed each time a neighbor item is collected")]
    public int damagePerHit = 1;

    [Tooltip("Off: only the cells to the left and right count as neighbors. On: up and down count too.")]
    public bool includeVerticalNeighbors = false;

    private bool isCheckingIsolatedBarrels; // Guards against re-entrance: breaking a barrel fires OnBoardChanged again

    private void Awake()
    {
        boardManager ??= FindFirstObjectByType<BoardManager>();
    }

    public void Initialize(BoardManager board)
    {
        boardManager = board;
    }

    private void OnEnable()
    {
        GameEvents.OnItemRemovedFromBoard += HandleItemRemovedFromBoard;
        GameEvents.OnBoardChanged += CheckIsolatedBarrels;
    }

    private void OnDisable()
    {
        GameEvents.OnItemRemovedFromBoard -= HandleItemRemovedFromBoard;
        GameEvents.OnBoardChanged -= CheckIsolatedBarrels;
    }

    /// <summary>
    /// 'item' left the board from 'cell' (tap or Match booster): damage every barrel
    /// sitting at an exit next to that cell.
    /// </summary>
    public void HandleItemRemovedFromBoard(Item item, Pipe sourcePipe, Vector2Int cell)
    {
        if (boardManager == null) return;

        // Snapshot: damaging can break a barrel and change the board while we iterate
        foreach (Item barrel in GetBarrelsAtExits().ToList())
        {
            if (barrel == item || barrel.parentPipe == sourcePipe) continue;
            if (!AreNeighbors(barrel.gridPosition, cell)) continue;

            boardManager.DamageBarrel(barrel, damagePerHit);
        }
    }

    /// <summary>
    /// Breaks any barrel at an exit whose neighbor pipes have nothing left to collect,
    /// since nothing could ever damage it again.
    /// </summary>
    public void CheckIsolatedBarrels()
    {
        if (boardManager == null || isCheckingIsolatedBarrels) return;

        isCheckingIsolatedBarrels = true;
        try
        {
            foreach (Item barrel in GetBarrelsAtExits().ToList())
            {
                if (!barrel.IsBarrel) continue; // Might have been broken earlier in this same loop

                bool anyNeighborHasItems = GetNeighborPipes(barrel).Any(p => !p.IsEmpty);
                if (!anyNeighborHasItems)
                {
                    boardManager.BreakBarrel(barrel);
                }
            }
        }
        finally
        {
            isCheckingIsolatedBarrels = false;
        }
    }

    // Barrels that are currently the front element of their pipe (sitting on the exit)
    private IEnumerable<Item> GetBarrelsAtExits()
    {
        return boardManager.Pipes
            .Where(p => p.HasPath && p.ExitItem != null && p.ExitItem.IsBarrel)
            .Select(p => p.ExitItem);
    }

    // Other pipes whose exit is a neighbor cell of the barrel
    private IEnumerable<Pipe> GetNeighborPipes(Item barrel)
    {
        return boardManager.Pipes
            .Where(p => p != barrel.parentPipe && p.HasPath && AreNeighbors(p.ExitPosition, barrel.gridPosition));
    }

    /// <summary>True if both cells are adjacent according to the current neighbor rule.</summary>
    public bool AreNeighbors(Vector2Int a, Vector2Int b)
    {
        Vector2Int delta = a - b;

        bool horizontal = delta.y == 0 && Mathf.Abs(delta.x) == 1;
        bool vertical = delta.x == 0 && Mathf.Abs(delta.y) == 1;

        return horizontal || (includeVerticalNeighbors && vertical);
    }
}
