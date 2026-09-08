public interface IBooster
{
    BoosterType Type { get; }

    /// <summary>
    /// Whether the booster can currently be applied to the game state (e.g. tray not empty).
    /// Does NOT check inventory count, that's the BoosterManager's responsibility.
    /// </summary>
    bool CanExecute();

    void Execute();
}
