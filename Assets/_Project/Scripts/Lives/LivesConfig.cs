using UnityEngine;

/// <summary>
/// Designer-tunable numbers of the lives system (Assets > Create > Lives > Lives Config).
/// The UserDataManager of [CORE_MANAGERS] uses the asset in Data/LivesConfig. A candidate for Remote Config.
/// </summary>
[CreateAssetMenu(fileName = "LivesConfig", menuName = "Lives/Lives Config")]
public class LivesConfig : ScriptableObject
{
    [Tooltip("Lives when full")]
    [Min(1)] public int maxLives = 5;

    [Tooltip("Minutes to get one life back. Counted with the game closed too.")]
    [Min(1)] public int minutesPerLife = 30;

    [Tooltip("Coins one life costs. Refilling costs this times the missing lives.")]
    [Min(0)] public int coinsPerLife = 20;

    public LivesRules Rules => new LivesRules(maxLives, minutesPerLife * 60, coinsPerLife);
}
