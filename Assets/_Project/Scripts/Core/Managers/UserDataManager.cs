using System;
using UnityEngine;

public class UserDataManager : MonoBehaviour, IUserDataProvider, IBoosterInventory, IProfileRepository
{
    public static UserDataManager Instance { get; private set; }

    [Tooltip("Numbers of the lives system (Data/LivesConfig). Without it the defaults are used: 5 lives, 30 minutes, 20 coins.")]
    [SerializeField] private LivesConfig livesConfig;
    [Tooltip("Coins for winning a level (Data/RewardsConfig). Without it: 4.")]
    [SerializeField] private RewardsConfig rewardsConfig;

    public UserProfile Profile { get; private set; }
    private IUserDataService _dataService;
    private LivesService _lives;
    private LevelRewards _rewards;

    /// <summary>The player's lives, kept in this profile (so they are saved and synced with it).</summary>
    public ILivesService Lives => _lives;

    public int CurrentLevel => Profile != null ? Profile.CurrentLevel : 1;
    public int Coins => Profile != null ? Profile.Coins : 0;
    public string UserId => Profile != null ? Profile.UserId : "";

    /// <summary>Raised after every save on the device. The cloud sync listens to it.</summary>
    public event Action ProfileSaved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // The device copy is always the one the game uses; CloudProfileSync keeps the cloud one equal
        _dataService = new LocalJsonUserDataService();
        Profile = _dataService.LoadProfile();

        LivesRules rules = livesConfig != null ? livesConfig.Rules : LivesRules.Default;
        _lives = new LivesService(rules, () => Profile, this, () => Save(), new SystemClock());
        _rewards = LevelRewards.From(rewardsConfig);
    }

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += AddLevelAndCoins;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= AddLevelAndCoins;
    }

    /// <summary>
    /// Level won: next level and the coin reward, saved together right away (closing the app on the win
    /// popup doesn't lose them, and there is nothing to "collect").
    /// </summary>
    private void AddLevelAndCoins()
    {
        int coins = (_rewards ?? LevelRewards.From(rewardsConfig)).CoinsForWin(Profile.CurrentLevel);

        Profile.CurrentLevel++;
        Profile.Coins += coins;

        Save();
        Debug.Log($"Saved. CurrentLvl: {Profile.CurrentLevel}, Coins: {Profile.Coins} (+{coins})");

        if (coins > 0) GameEvents.TriggerCoinsChanged(Profile.Coins);
        GameEvents.TriggerLevelRewarded(coins);
    }

    /// <summary>
    /// Saves on the device and tells the listeners (cloud sync).
    /// touch: stamps the save time. False only when taking the cloud copy, which keeps its own.
    /// </summary>
    private void Save(bool touch = true)
    {
        if (touch) Profile.UpdatedAtTicks = DateTime.UtcNow.Ticks;

        _dataService.SaveProfile(Profile);
        ProfileSaved?.Invoke();
    }

    // --- IBoosterInventory ---

    public int GetCount(BoosterType type)
    {
        return Profile != null ? Profile.GetBoosterCount(type) : 0;
    }

    public bool TryConsume(BoosterType type)
    {
        int current = GetCount(type);
        if (current <= 0) return false;

        Profile.SetBoosterCount(type, current - 1);
        Save();
        return true;
    }

    public void Add(BoosterType type, int amount)
    {
        if (amount <= 0 || Profile == null) return;

        Profile.SetBoosterCount(type, GetCount(type) + amount);
        Save();
    }

    public void EnsureStarterStock(BoosterType type, int amount)
    {
        if (Profile == null || Profile.HasBoosterEntry(type)) return;

        Profile.SetBoosterCount(type, amount);
        Save();
    }

    // --- Currency spending (used to buy boosters once free stock runs out) ---

    public bool TrySpendCoins(int amount)
    {
        if (Profile == null || Profile.Coins < amount) return false;

        Profile.Coins -= amount;
        Save();
        GameEvents.TriggerCoinsChanged(Profile.Coins);
        return true;
    }

    // Only sets it if empty, so signing in later never relinks a player's existing local progress.
    // Used only when the cloud is off: with the cloud on, CloudProfileSync sets the owner (SetOwner).
    public void SetUserId(string userId)
    {
        if (Profile == null || string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(Profile.UserId)) return;

        Profile.UserId = userId;
        Save();
    }

    // --- IProfileRepository (cloud sync) ---

    public void ReplaceProfile(UserProfile profile)
    {
        if (profile == null) return;

        Profile = profile;
        Save(touch: false);
        GameEvents.TriggerCoinsChanged(Profile.Coins);
        _lives?.NotifyChanged();
    }

    public void SetOwner(string playerId)
    {
        if (Profile == null || string.IsNullOrEmpty(playerId) || Profile.UserId == playerId) return;

        Profile.UserId = playerId;
        Save(touch: false);
    }

    public void ResetData()
    {
        // New profile applying default values
        Profile = new UserProfile();

        Save();
        GameEvents.TriggerCoinsChanged(Profile.Coins);
        _lives?.NotifyChanged();

        Debug.Log($"[DataReset] CurrentLvl: {Profile.CurrentLevel}, Coins: {Profile.Coins}");
    }
}
