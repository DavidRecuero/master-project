/// <summary>
/// Every kind of vibration the game can ask for. Gameplay and UI only name the kind; HapticPresets
/// decides what each one feels like, so the whole game can be retuned from one place.
/// Names follow the usual mobile haptics vocabulary (selection, impacts, notifications).
/// Append new ids at the end: Unity stores enums as numbers, so inserting one shifts the rest.
/// </summary>
public enum HapticType
{
    None = 0,

    /// <summary>The smallest tick. Picking an item.</summary>
    Selection = 1,

    // Impacts: one pulse, three strengths
    LightImpact = 2,
    MediumImpact = 3,
    HeavyImpact = 4,

    // Notifications: short patterns with a meaning
    Success = 5,
    Warning = 6,
    Failure = 7
}
