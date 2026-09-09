using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Simplified "instant match" booster:
///  - Usable as long as any item remains in play, in the tray or the board.
///  - Target color = the most abundant color currently in the tray, or (if the tray is
///    empty) a random color among whatever is still active on the board.
///  - Pulls items of that color from anywhere on the board (not just pipe exits) until
///    the match completes, the tray fills up, or the board runs out of that color.
/// </summary>
public class MatchBooster : IBooster
{
    public BoosterType Type => BoosterType.Match;

    private readonly TrayManager _trayManager;
    private readonly BoardManager _boardManager;

    public MatchBooster(TrayManager trayManager, BoardManager boardManager)
    {
        _trayManager = trayManager;
        _boardManager = boardManager;
    }

    public bool CanExecute()
    {
        if (_trayManager == null || _boardManager == null) return false;
        return _trayManager.TrayItems.Count > 0 || !_boardManager.IsBoardCleared();
    }

    public void Execute()
    {
        if (!CanExecute()) return;

        int? targetColor = GetTargetColor();
        if (targetColor == null) return;

        int alreadyInTray = _trayManager.TrayItems.Count(item => item.colorID == targetColor);
        int needed = Mathf.Max(0, _trayManager.matchSize - alreadyInTray);

        for (int i = 0; i < needed; i++)
        {
            if (_trayManager.IsFull) break;

            Item item = _boardManager.ExtractAnyItemOfColor(targetColor.Value);
            if (item == null) break; // no more of that color left on the board

            _trayManager.TryAddItem(item);
        }
    }

    private int? GetTargetColor()
    {
        // Priority 1: the most abundant color already waiting in the tray
        var trayGroup = _trayManager.TrayItems
            .GroupBy(item => item.colorID)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (trayGroup != null) return trayGroup.Key;

        // Priority 2: a random color among whatever is still active on the board
        List<int> boardColors = _boardManager.GetActiveColors();
        return boardColors.Count > 0 ? boardColors[Random.Range(0, boardColors.Count)] : (int?)null;
    }
}