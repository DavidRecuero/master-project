/// <summary>
/// Reshuffles the colors of every item currently spawned across all pipes on the board,
/// giving the player a fresh distribution without changing the board layout.
/// </summary>
public class ShuffleBooster : IBooster
{
    public BoosterType Type => BoosterType.Shuffle;

    private readonly BoardManager _boardManager;

    public ShuffleBooster(BoardManager boardManager)
    {
        _boardManager = boardManager;
    }

    public bool CanExecute() => _boardManager != null;

    public void Execute() => _boardManager.ShuffleActiveItems();
}
