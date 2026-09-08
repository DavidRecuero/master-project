using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoosterPrice
{
    public BoosterType type;
    public int coinCost = 50;
}

public class BoosterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrayManager trayManager;
    [SerializeField] private BoardManager boardManager;

    [Header("Coin cost once free stock runs out")]
    [SerializeField]
    private List<BoosterPrice> prices = new List<BoosterPrice>
    {
        new BoosterPrice { type = BoosterType.Match, coinCost = 50 },
        new BoosterPrice { type = BoosterType.Shuffle, coinCost = 40 },
        new BoosterPrice { type = BoosterType.TrayClearer, coinCost = 75 },
    };

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
    }

    public int GetRemainingFreeUses(BoosterType type)
    {
        return _inventory != null ? _inventory.GetCount(type) : 0;
    }

    public int GetCoinPrice(BoosterType type)
    {
        BoosterPrice price = prices.Find(p => p.type == type);
        return price != null ? price.coinCost : 0;
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

    // Convenience wrappers so UI Buttons can call them directly without params
    public void UseMatch() => UseBooster(BoosterType.Match);
    public void UseShuffle() => UseBooster(BoosterType.Shuffle);
    public void UseTrayClearer() => UseBooster(BoosterType.TrayClearer);
}
