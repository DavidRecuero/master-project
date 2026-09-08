using System.Collections.Generic;

/// <summary>
/// Empties the tray, sending each item back onto the board into a random pipe
/// instead of destroying them. Useful when the player is close to a tray overflow.
/// </summary>
public class TrayClearerBooster : IBooster
{
    public BoosterType Type => BoosterType.TrayClearer;

    private readonly TrayManager _trayManager;
    private readonly BoardManager _boardManager;

    public TrayClearerBooster(TrayManager trayManager, BoardManager boardManager)
    {
        _trayManager = trayManager;
        _boardManager = boardManager;
    }

    public bool CanExecute()
    {
        return _trayManager != null && _boardManager != null && _trayManager.TrayItems.Count > 0;
    }

    public void Execute()
    {
        if (!CanExecute()) return;

        // Snapshot first, since RemoveItemFromTray mutates the live tray list while we iterate
        List<Item> itemsToRedistribute = new List<Item>(_trayManager.TrayItems);

        foreach (Item item in itemsToRedistribute)
        {
            int colorId = item.colorID;
            _trayManager.RemoveItemFromTray(item);
            _boardManager.AddItemToRandomPipe(colorId);
        }
    }
}
