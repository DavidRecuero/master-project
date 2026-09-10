using UnityEngine;

/// <summary>
/// Designer-tunable configuration for a single booster: how many free uses a new
/// player starts with, and how many coins it costs once that free stock runs out.
/// Create one asset per BoosterType (Assets > Create > Boosters > Booster Definition).
/// </summary>
[CreateAssetMenu(fileName = "BoosterDefinition", menuName = "Boosters/Booster Definition")]
public class BoosterDefinition : ScriptableObject
{
    public BoosterType type;
    public string displayName;
    public Sprite icon;

    [Header("Economy")]
    [Min(0)] public int freeStarterCount = 15;
    [Min(0)] public int coinPrice = 50;
}