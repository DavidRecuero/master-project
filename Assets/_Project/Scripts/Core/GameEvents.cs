using System;

public static class GameEvents
{
    public static event Action OnLevelCleared;
    public static event Action OnLevelFailed;
    public static event Action<BoosterType> OnBoosterUsed;

    public static void TriggerLevelCleared() => OnLevelCleared?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerBoosterUsed(BoosterType type) => OnBoosterUsed?.Invoke(type);
}