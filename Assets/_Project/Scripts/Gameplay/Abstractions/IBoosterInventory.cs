public interface IBoosterInventory
{
    int GetCount(BoosterType type);

    /// <summary>
    /// Tries to spend one unit of the given booster. Returns false if there's none left.
    /// </summary>
    bool TryConsume(BoosterType type);

    void Add(BoosterType type, int amount);

    /// <summary>
    /// Grants the starter free stock for a booster type, but only the first time it's
    /// ever called for that type (won't re-grant after the player has spent it down to 0).
    /// </summary>
    void EnsureStarterStock(BoosterType type, int amount);
}