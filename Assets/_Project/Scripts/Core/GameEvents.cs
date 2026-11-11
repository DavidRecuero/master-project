using System;

public static class GameEvents
{
    public static event Action OnLevelCleared;
    public static event Action OnLevelFailed;
    /// <summary>The player abandoned the level from Settings (it counts as lost). Raised just before going back to the menu.</summary>
    public static event Action OnLevelAbandoned;
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
    /// <summary>Three (or more) items of one color just matched in the tray. The int is their colorID.</summary>
    public static event Action<int> OnMatchCompleted;
    /// <summary>An item reached its slot in the tray (after the fly-in animation).</summary>
    public static event Action<Item> OnItemLandedInTray;
    /// <summary>A hidden item showed its real color when it entered the tray.</summary>
    public static event Action<Item> OnHiddenItemRevealed;
    /// <summary>The fog moved one step.</summary>
    public static event Action OnFogMoved;
    /// <summary>The player tried a booster with no free stock and not enough coins.</summary>
    public static event Action<BoosterType> OnBoosterDenied;
    /// <summary>The player got the reward of the level just won. The int is the coins given (already added and saved).</summary>
    public static event Action<int> OnLevelRewarded;
    /// <summary>The player changed a privacy choice (consent popup, Settings, debug reset). The bool is the new "may use".</summary>
    public static event Action<ConsentPurpose, bool> OnConsentChanged;
    /// <summary>A booster was paid with coins (no free stock left). Raised just before OnBoosterUsed. The int is the price.</summary>
    public static event Action<BoosterType, int> OnBoosterPurchased;
    /// <summary>Lives were bought with coins. The ints are the lives given and the coins paid.</summary>
    public static event Action<int, int> OnLivesBought;
    /// <summary>The player tried to play with no lives left (the "out of lives" popup opens).</summary>
    public static event Action OnOutOfLives;

    public static void TriggerLevelCleared() => OnLevelCleared?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerLevelAbandoned() => OnLevelAbandoned?.Invoke();
    public static void TriggerBoosterUsed(BoosterType type) => OnBoosterUsed?.Invoke(type);
    public static void TriggerTrayChanged() => OnTrayChanged?.Invoke();
    public static void TriggerBoardChanged() => OnBoardChanged?.Invoke();
    public static void TriggerItemCollectedByPlayer(Item item, Pipe pipe) => OnItemCollectedByPlayer?.Invoke(item, pipe);
    public static void TriggerItemRemovedFromBoard(Item item, Pipe pipe, UnityEngine.Vector2Int cell) => OnItemRemovedFromBoard?.Invoke(item, pipe, cell);
    public static void TriggerLevelLoaded(LevelData data) => OnLevelLoaded?.Invoke(data);
    public static void TriggerBarrelDamaged(Item barrel, int livesLeft) => OnBarrelDamaged?.Invoke(barrel, livesLeft);
    public static void TriggerCoinsChanged(int balance) => OnCoinsChanged?.Invoke(balance);
    public static void TriggerCoinStoreRequested() => OnCoinStoreRequested?.Invoke();
    public static void TriggerMatchCompleted(int colorId) => OnMatchCompleted?.Invoke(colorId);
    public static void TriggerItemLandedInTray(Item item) => OnItemLandedInTray?.Invoke(item);
    public static void TriggerHiddenItemRevealed(Item item) => OnHiddenItemRevealed?.Invoke(item);
    public static void TriggerFogMoved() => OnFogMoved?.Invoke();
    public static void TriggerBoosterDenied(BoosterType type) => OnBoosterDenied?.Invoke(type);
    public static void TriggerLevelRewarded(int coins) => OnLevelRewarded?.Invoke(coins);
    public static void TriggerConsentChanged(ConsentPurpose purpose, bool granted) => OnConsentChanged?.Invoke(purpose, granted);
    public static void TriggerBoosterPurchased(BoosterType type, int price) => OnBoosterPurchased?.Invoke(type, price);
    public static void TriggerLivesBought(int lives, int price) => OnLivesBought?.Invoke(lives, price);
    public static void TriggerOutOfLives() => OnOutOfLives?.Invoke();
}