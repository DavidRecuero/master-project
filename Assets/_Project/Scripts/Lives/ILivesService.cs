using System;

/// <summary>
/// The player's lives. Classic rules:
///  - Starting a level uses a life; winning it gives the life back. So losing, abandoning or closing the game
///    in the middle of a level costs a life, and killing the app can't be used to dodge a defeat.
///  - Missing lives come back one at a time every few minutes, also with the game closed (it is worked out
///    from the time saved in the profile, not from a running timer).
///  - Lives can be bought with coins: one, or all the missing ones at once.
/// Menus and gameplay ask this interface, so tests can swap in a fake.
/// </summary>
public interface ILivesService
{
    /// <summary>Lives the player has now (regeneration included).</summary>
    int Lives { get; }

    int MaxLives { get; }

    bool IsFull { get; }

    /// <summary>True if there is at least one life to start a level.</summary>
    bool HasLives { get; }

    /// <summary>Time left for the next life. Zero when full.</summary>
    TimeSpan TimeToNextLife { get; }

    /// <summary>Coins of one life.</summary>
    int LifePrice { get; }

    /// <summary>Coins to fill every missing life (LifePrice x missing lives). Zero when full.</summary>
    int RefillPrice { get; }

    /// <summary>True if the player has at least that many coins.</summary>
    bool CanAfford(int price);

    /// <summary>Uses a life to start a level. False (and nothing changes) if there are no lives.</summary>
    bool TryUseLife();

    /// <summary>Gives back the life of a level the player won. Never goes over the maximum.</summary>
    void ReturnLife();

    /// <summary>Buys one life with coins. False if full or the player can't pay (nothing is spent).</summary>
    bool TryBuyLife();

    /// <summary>Buys every missing life with coins. False if full or the player can't pay (nothing is spent).</summary>
    bool TryRefill();

    /// <summary>Raised after lives are used, given back or bought, and when the profile is replaced.</summary>
    event Action LivesChanged;
}
