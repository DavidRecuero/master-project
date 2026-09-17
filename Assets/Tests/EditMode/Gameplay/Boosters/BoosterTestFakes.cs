using System.Collections.Generic;

public class FakeBoosterInventory : IBoosterInventory
{
    private readonly Dictionary<BoosterType, int> _counts = new Dictionary<BoosterType, int>();
    private readonly HashSet<BoosterType> _seeded = new HashSet<BoosterType>();

    public int TryConsumeCallCount { get; private set; }
    public int EnsureStarterStockCallCount { get; private set; }

    public int GetCount(BoosterType type) => _counts.TryGetValue(type, out int count) ? count : 0;

    public bool TryConsume(BoosterType type)
    {
        TryConsumeCallCount++;

        int current = GetCount(type);
        if (current <= 0) return false;

        _counts[type] = current - 1;
        return true;
    }

    public void Add(BoosterType type, int amount)
    {
        _counts[type] = GetCount(type) + amount;
    }

    public void EnsureStarterStock(BoosterType type, int amount)
    {
        EnsureStarterStockCallCount++;

        if (_seeded.Contains(type)) return;

        _seeded.Add(type);
        _counts[type] = amount;
    }

    // Test helper: set a count directly without going through the "seed once" rule
    public void SetCount(BoosterType type, int amount)
    {
        _counts[type] = amount;
        _seeded.Add(type);
    }
}
