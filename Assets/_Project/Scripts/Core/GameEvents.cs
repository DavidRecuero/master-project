using System;

public static class GameEvents
{
    public static event Action OnLevelCleared;
    public static event Action OnLevelFailed;
    public static event Action<BoosterType> OnBoosterUsed;
    public static event Action OnTrayChanged;
    public static event Action OnBoardChanged;

    public static void TriggerLevelCleared() => OnLevelCleared?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerBoosterUsed(BoosterType type) => OnBoosterUsed?.Invoke(type);
    public static void TriggerTrayChanged() => OnTrayChanged?.Invoke();
    public static void TriggerBoardChanged() => OnBoardChanged?.Invoke();
}