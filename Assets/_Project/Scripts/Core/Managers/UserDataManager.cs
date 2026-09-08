using UnityEngine;

public class UserDataManager : MonoBehaviour, IUserDataProvider, IBoosterInventory
{
    public static UserDataManager Instance { get; private set; }

    public UserProfile Profile { get; private set; }
    private IUserDataService _dataService;

    public int CurrentLevel => Profile != null ? Profile.CurrentLevel : 1;
    public int Coins => Profile != null ? Profile.Coins : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Local service, to switch for UGS/Playfab
        _dataService = new LocalJsonUserDataService();
        Profile = _dataService.LoadProfile();
    }

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += AddLevelAndCoins;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= AddLevelAndCoins;
    }

    private void AddLevelAndCoins()
    {
        Profile.CurrentLevel++;

        _dataService.SaveProfile(Profile);
        Debug.Log($"Saved. CurrentLvl: {Profile.CurrentLevel}, Coins: {Profile.Coins}");
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
        _dataService.SaveProfile(Profile);
        return true;
    }

    public void Add(BoosterType type, int amount)
    {
        if (amount <= 0 || Profile == null) return;

        Profile.SetBoosterCount(type, GetCount(type) + amount);
        _dataService.SaveProfile(Profile);
    }

    // --- Currency spending (used to buy boosters once free stock runs out) ---

    public bool TrySpendCoins(int amount)
    {
        if (Profile == null || Profile.Coins < amount) return false;

        Profile.Coins -= amount;
        _dataService.SaveProfile(Profile);
        return true;
    }

    public void ResetData()
    {
        // New profile applying default values
        Profile = new UserProfile();

        _dataService.SaveProfile(Profile);

        Debug.Log($"[DataReset] CurrentLvl: {Profile.CurrentLevel}, Coins: {Profile.Coins}");
    }
}