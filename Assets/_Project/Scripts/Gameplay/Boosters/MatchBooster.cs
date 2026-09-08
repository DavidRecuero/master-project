using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Instantly completes a match of 3, without the player tapping anything.
/// Priority order:
///   1) If the tray already holds 1 or 2 items of some color, auto-collect the missing
///      amount from pipe exits to complete the match. Colors closer to completion
///      (a pair over a single) are preferred, since they need fewer pipe items.
///   2) Otherwise, look for a color that currently has enough items available at the
///      exit of different pipes to form a full match of 3 by itself.
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

    public bool CanExecute() => FindMatchPlan() != null;

    public void Execute()
    {
        List<Item> plan = FindMatchPlan();
        if (plan == null) return;

        foreach (Item item in plan)
        {
            _trayManager.CollectItem(item);
        }
    }

    // Returns the pipe-front items that need to be auto-collected to complete a match,
    // or null if no match is currently achievable.
    private List<Item> FindMatchPlan()
    {
        if (_trayManager == null || _boardManager == null) return null;

        List<Item> frontItems = _boardManager.GetFrontItems().Where(i => !i.inTray).ToList();

        // Priority 1: complete a color already waiting in the tray (1 or 2 items),
        // checking the most-complete groups first so a pair beats a single leftover.
        var trayGroupsByCount = _trayManager.TrayItems
            .GroupBy(i => i.colorID)
            .OrderByDescending(g => g.Count());

        foreach (var group in trayGroupsByCount)
        {
            int needed = _trayManager.matchSize - group.Count();
            if (needed <= 0) continue; // shouldn't happen (would've already matched), just a safety guard

            List<Item> candidates = frontItems.Where(i => i.colorID == group.Key).Take(needed).ToList();
            if (candidates.Count == needed) return candidates;
        }

        // Priority 2: 3 same-color items already available at the front of different pipes
        var boardGroup = frontItems
            .GroupBy(i => i.colorID)
            .FirstOrDefault(g => g.Count() >= _trayManager.matchSize);

        return boardGroup?.Take(_trayManager.matchSize).ToList();
    }
}
