using System;

public static class GameEvents
{
    public static event Action OnLevelCleared;
    public static event Action OnLevelFailed;
    public static event Action<BoosterType> OnBoosterUsed;
    public static event Action OnTrayChanged;
    public static event Action OnBoardChanged;
    public static event Action<Item, Pipe> OnItemCollectedByPlayer;
    public static event Action<Item, Pipe, UnityEngine.Vector2Int> OnItemRemovedFromBoard;
    public static event Action<LevelData> OnLevelLoaded;
    /// <summary>A barrel lost lives. The int is the lives left: 0 means it just broke open.</summary>
    public static event Action<Item, int> OnBarrelDamaged;
    /// <summary>The player's coin balance changed. The int is the new balance.</summary>
    public static event Action<int> OnCoinsChanged;
    /// <summary>The player asked for more coins (HUD "+" button). The store / rewarded ad flow listens here.</summary>
    public static event Action OnCoinStoreRequested;

    public static void TriggerLevelCleared() => OnLevelCleared?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerBoosterUsed(BoosterType type) => OnBoosterUsed?.Invoke(type);
    public static void TriggerTrayChanged() => OnTrayChanged?.Invoke();
    public static void TriggerBoardChanged() => OnBoardChanged?.Invoke();
    public static void TriggerItemCollectedByPlayer(Item item, Pipe pipe) => OnItemCollectedByPlayer?.Invoke(item, pipe);
    public static void TriggerItemRemovedFromBoard(Item item, Pipe pipe, UnityEngine.Vector2Int cell) => OnItemRemovedFromBoard?.Invoke(item, pipe, cell);
    public static void TriggerLevelLoaded(LevelData data) => OnLevelLoaded?.Invoke(data);
    public static void TriggerBarrelDamaged(Item barrel, int livesLeft) => OnBarrelDamaged?.Invoke(barrel, livesLeft);
    public static void TriggerCoinsChanged(int balance) => OnCoinsChanged?.Invoke(balance);
    public static void TriggerCoinStoreRequested() => OnCoinStoreRequested?.Invoke();
}