/// <summary>
/// Every sound effect the game can play. The SFX file names match these ids with an "Sfx_" prefix
/// (Sfx_Match3.wav -> Match3), and variants end in _01, _02... (Sfx_ItemTap_01.wav -> ItemTap).
/// Tools > Audio > Build Sound Library relies on that naming to fill the SoundLibrary.
/// Append new ids at the end: Unity stores enums as numbers, so inserting one shifts the rest.
/// </summary>
public enum SfxId
{
    None = 0,

    // Gameplay
    ItemTap = 1,
    ItemLand = 2,
    Match3 = 3,
    LevelWin = 4,
    LevelLose = 5,

    // UI
    ButtonClick = 6,
    PlayButton = 7,
    PopupOpen = 8,

    // Boosters
    BoosterMatch = 9,
    BoosterShuffle = 10,
    BoosterTrayClear = 11,
    BoosterDenied = 12,

    // Obstacles
    BarrelHit = 13,
    BarrelBreak = 14,
    FogMove = 15,
    HiddenReveal = 16,

    // Economy and warnings
    CoinsGain = 17,
    CoinsSpend = 18,
    TrayWarning = 19
}
