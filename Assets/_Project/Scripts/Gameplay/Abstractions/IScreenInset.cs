/// <summary>
/// A screen-space UI strip the board must not go under (booster band at the bottom, HUD bar at the top).
/// The camera only needs how much of the screen height it takes, not what it is.
/// </summary>
public interface IScreenInset
{
    /// <summary>Part of the screen height covered, from its screen edge (0..1).</summary>
    float ScreenFraction { get; }
}
