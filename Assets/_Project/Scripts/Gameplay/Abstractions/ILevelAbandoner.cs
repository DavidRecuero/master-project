/// <summary>
/// Leaves the level in progress on purpose (Settings > Abandon level): the level counts as lost
/// and the player goes back to the main menu. Implemented by the GameManager; the confirmation popup
/// only knows this interface, so the tests can use a fake.
/// </summary>
public interface ILevelAbandoner
{
    /// <summary>
    /// True if the level was abandoned. False if there was nothing to abandon
    /// (the level had already been won or lost, or it was already being abandoned).
    /// </summary>
    bool AbandonLevel();
}
