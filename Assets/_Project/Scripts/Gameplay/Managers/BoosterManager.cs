using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoosterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrayManager trayManager;
    [SerializeField] private BoardManager boardManager;

    [Header("Config")]
    [SerializeField] private BoosterDefinition[] boosterDefinitions;

    private IBoosterInventory _inventory;
    private IUserDataProvider _currency;
    private Dictionary<BoosterType, IBooster> _boosters;

    public void Initialize(IBoosterInventory inventory, IUserDataProvider currency)
    {
        _inventory = inventory;
        _currency = currency;
    }

    private void Awake()
    {
        trayManager ??= FindFirstObjectByType<TrayManager>();
        boardManager ??= FindFirstObjectByType<BoardManager>();

        _inventory ??= UserDataManager.Instance;
        _currency ??= UserDataManager.Instance;

        _boosters = new Dictionary<BoosterType, IBooster>
        {
            { BoosterType.Match, new MatchBooster(trayManager, boardManager) },
            { BoosterType.Shuffle, new ShuffleBooster(boardManager) },
            { BoosterType.TrayClearer, new TrayClearerBooster(trayManager, boardManager) }
        };

        GrantStarterStockFromDefinitions();
    }

    // Only actually grants stock the first time it's called for each type (see
    // IBoosterInventory.EnsureStarterStock) - safe to call every time this wakes up.
    private void GrantStarterStockFromDefinitions()
    {
        if (_inventory == null || boosterDefinitions == null) return;

        foreach (BoosterDefinition definition in boosterDefinitions)
        {
            if (definition == null) continue;
            _inventory.EnsureStarterStock(definition.type, definition.freeStarterCount);
        }
    }

    private BoosterDefinition GetDefinition(BoosterType type)
    {
        return boosterDefinitions?.FirstOrDefault(d => d != null && d.type == type);
    }

    public int GetRemainingFreeUses(BoosterType type)
    {
        return _inventory != null ? _inventory.GetCount(type) : 0;
    }

    public int GetCoinPrice(BoosterType type)
    {
        BoosterDefinition definition = GetDefinition(type);
        return definition != null ? definition.coinPrice : 0;
    }

    /// <summary>
    /// Checks if the booster could be applied to the current game state (e.g. tray not
    /// empty for TrayClearer), WITHOUT looking at inventory or coins.
    /// </summary>
    public bool CanUseBooster(BoosterType type)
    {
        return _boosters.TryGetValue(type, out IBooster booster) && booster.CanExecute();
    }

    /// <summary>
    /// Attempts to use a booster. Consumes free inventory first; if there's none left,
    /// falls back to charging the player in coins. Returns true only if the booster
    /// actually ran (there was something valid to do AND the player could pay for it).
    /// </summary>
    public bool UseBooster(BoosterType type)
    {
        if (_inventory == null || _currency == null)
        {
            Debug.LogError("❌ [BoosterManager] Missing inventory/currency dependencies.");
            return false;
        }

        if (!_boosters.TryGetValue(type, out IBooster booster) || !booster.CanExecute())
        {
            Debug.Log($"[BoosterManager] Booster {type} can't be applied to the current state.");
            return false;
        }

        bool paidWithFreeStock = _inventory.TryConsume(type);

        if (!paidWithFreeStock)
        {
            int price = GetCoinPrice(type);
            if (!_currency.TrySpendCoins(price))
            {
                Debug.Log($"[BoosterManager] No free {type} left and not enough coins (needs {price}).");
                // TODO: hook here to open the coin store / rewarded ad flow
                return false;
            }
        }

        booster.Execute();
        GameEvents.TriggerBoosterUsed(type);
        return true;
    }
}