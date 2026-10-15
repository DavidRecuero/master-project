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

    public static void TriggerLevelCleared() => OnLevelCleared?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerBoosterUsed(BoosterType type) => OnBoosterUsed?.Invoke(type);
    public static void TriggerTrayChanged() => OnTrayChanged?.Invoke();
    public static void TriggerBoardChanged() => OnBoardChanged?.Invoke();
    public static void TriggerItemCollectedByPlayer(Item item, Pipe pipe) => OnItemCollectedByPlayer?.Invoke(item, pipe);
    public static void TriggerItemRemovedFromBoard(Item item, Pipe pipe, UnityEngine.Vector2Int cell) => OnItemRemovedFromBoard?.Invoke(item, pipe, cell);
}