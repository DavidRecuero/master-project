using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }

    public UserProfile Profile { get; private set; }
    private IUserDataService _dataService;

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

    public void ResetData()
    {
        // New profile applying default values
        Profile = new UserProfile();

        _dataService.SaveProfile(Profile);

        Debug.Log($"[DataReset] CurrentLvl: {Profile.CurrentLevel}, Coins: {Profile.Coins}");
    }
}