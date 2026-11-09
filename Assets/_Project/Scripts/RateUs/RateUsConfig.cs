using UnityEngine;

/// <summary>
/// Designer-tunable numbers of the Rate Us prompt (Assets > Create > Rate Us > Rate Us Config).
/// The RateUsTrigger of the Level scene uses the asset in Data/RateUsConfig. A candidate for Remote Config.
/// </summary>
[CreateAssetMenu(fileName = "RateUsConfig", menuName = "Rate Us/Rate Us Config")]
public class RateUsConfig : ScriptableObject
{
    [Tooltip("The first card can show when a level from this one on is won")]
    [Min(1)] public int firstPromptLevel = 8;

    [Tooltip("Times the card is asked for at most, per device")]
    [Min(0)] public int maxPrompts = 3;

    [Tooltip("Days between one request and the next")]
    [Min(0)] public int daysBetweenPrompts = 30;

    [Tooltip("Seconds after the win popup shows up. If the player leaves the result before, there is no card this time (it never shows over gameplay).")]
    [Min(0)] public float delayAfterWinSeconds = 0f;

    public RateUsRules Rules => new RateUsRules(firstPromptLevel, maxPrompts, daysBetweenPrompts, delayAfterWinSeconds);
}
