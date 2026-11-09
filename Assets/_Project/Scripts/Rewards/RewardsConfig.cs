using UnityEngine;

/// <summary>
/// Designer-tunable level rewards (Assets > Create > Rewards > Rewards Config).
/// The UserDataManager of [CORE_MANAGERS] uses the asset in Data/RewardsConfig. A candidate for Remote Config.
/// </summary>
[CreateAssetMenu(fileName = "RewardsConfig", menuName = "Rewards/Rewards Config")]
public class RewardsConfig : ScriptableObject
{
    public const int DefaultCoinsPerWin = 4;

    [Tooltip("Coins the player gets for winning any level")]
    [Min(0)] public int coinsPerWin = DefaultCoinsPerWin;
}
