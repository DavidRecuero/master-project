public enum GameState
{
    Playing,
    Victory,
    Defeat,
    /// <summary>The player left the level from Settings: it counts as lost, with no result popup.</summary>
    Abandoned
}