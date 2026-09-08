public interface IBoosterInventory
{
    int GetCount(BoosterType type);

    /// <summary>
    /// Tries to spend one unit of the given booster. Returns false if there's none left.
    /// </summary>
    bool TryConsume(BoosterType type);

    void Add(BoosterType type, int amount);
}
