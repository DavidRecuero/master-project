using System;

/// <summary>
/// The lives system on top of the UserProfile. Pure logic: the profile, the coins, the save and the clock
/// come from outside, so it is tested without scenes or waiting.
///
/// What is saved (two fields, so it syncs to the cloud with the rest of the profile):
///   MissingLives       - lives the player doesn't have. Stored as "missing" so a profile saved before the
///                        lives existed (0) loads full.
///   NextLifeStartTicks - UTC time when the countdown of the next life started. 0 when full.
/// The lives are worked out from those two fields and the clock every time they are read, so they come
/// back with the game closed. Regenerating only changes the profile in memory: it is the same result
/// whenever it is worked out, so it is saved with the next real change (no save every 30 minutes).
///
/// Device clock: lives use the device time (offline game, no server time), so changing the clock by hand
/// can speed them up, as in most offline games. A clock moved back never breaks the countdown: it starts
/// again from the new time (never a negative or longer-than-one-life wait).
/// </summary>
public sealed class LivesService : ILivesService
{
    private readonly LivesRules _rules;
    private readonly Func<UserProfile> _profile;
    private readonly IUserDataProvider _wallet;
    private readonly Action _save;
    private readonly IClock _clock;

    public event Action LivesChanged;

    /// <param name="profile">The profile in use. A function because the cloud sync can replace it.</param>
    /// <param name="wallet">Pays the lives (coins).</param>
    /// <param name="save">Saves the profile on the device (and the cloud sync uploads it).</param>
    public LivesService(LivesRules rules, Func<UserProfile> profile, IUserDataProvider wallet, Action save, IClock clock)
    {
        _rules = new LivesRules(rules.MaxLives, rules.SecondsPerLife, rules.CoinsPerLife);
        _profile = profile;
        _wallet = wallet;
        _save = save;
        _clock = clock ?? new SystemClock();
    }

    public int MaxLives => _rules.MaxLives;

    public int LifePrice => _rules.CoinsPerLife;

    public int Lives => MaxLives - Missing();

    public bool IsFull => Missing() == 0;

    public bool HasLives => Lives > 0;

    public int RefillPrice => Missing() * LifePrice;

    public TimeSpan TimeToNextLife
    {
        get
        {
            UserProfile p = Current();
            if (p == null || p.MissingLives == 0) return TimeSpan.Zero;

            long elapsed = Now - p.NextLifeStartTicks;
            long left = _rules.TimePerLife.Ticks - elapsed;
            return left > 0 ? TimeSpan.FromTicks(left) : TimeSpan.Zero;
        }
    }

    public bool CanAfford(int price) => _wallet != null && _wallet.Coins >= price;

    public bool TryUseLife()
    {
        UserProfile p = Current();
        if (p == null || p.MissingLives >= MaxLives) return false;

        if (p.MissingLives == 0) p.NextLifeStartTicks = Now;   // the countdown starts with the first missing life
        p.MissingLives++;

        Commit();
        return true;
    }

    public void ReturnLife()
    {
        UserProfile p = Current();
        if (p == null || p.MissingLives == 0) return;

        GiveLives(p, 1);
        Commit();
    }

    public bool TryBuyLife()
    {
        UserProfile p = Current();
        if (p == null || p.MissingLives == 0) return false;
        if (!Pay(LifePrice)) return false;

        GiveLives(p, 1);
        Commit();
        return true;
    }

    public bool TryRefill()
    {
        UserProfile p = Current();
        if (p == null || p.MissingLives == 0) return false;
        if (!Pay(p.MissingLives * LifePrice)) return false;

        GiveLives(p, p.MissingLives);
        Commit();
        return true;
    }

    /// <summary>The profile was replaced (cloud copy, account reset): tells the listeners to read the lives again.</summary>
    public void NotifyChanged() => LivesChanged?.Invoke();

    // ---------- Helpers ----------

    private long Now => _clock.UtcNow.Ticks;

    private int Missing()
    {
        UserProfile p = Current();
        return p != null ? p.MissingLives : 0;
    }

    /// <summary>The profile with the lives that came back since the last read already applied.</summary>
    private UserProfile Current()
    {
        UserProfile p = _profile?.Invoke();
        if (p != null) Regenerate(p);
        return p;
    }

    private void Regenerate(UserProfile p)
    {
        // Values out of range (older or edited saves, a smaller maximum from config): back into range
        if (p.MissingLives < 0) p.MissingLives = 0;
        if (p.MissingLives > MaxLives) p.MissingLives = MaxLives;

        if (p.MissingLives == 0)
        {
            p.NextLifeStartTicks = 0;
            return;
        }

        long now = Now;

        // No start saved, or the clock went back: the countdown starts again from now
        if (p.NextLifeStartTicks <= 0 || p.NextLifeStartTicks > now)
        {
            p.NextLifeStartTicks = now;
            return;
        }

        long perLife = _rules.TimePerLife.Ticks;
        long recovered = (now - p.NextLifeStartTicks) / perLife;
        if (recovered <= 0) return;

        int lives = recovered >= p.MissingLives ? p.MissingLives : (int)recovered;
        p.MissingLives -= lives;
        // Keeps the time already counted for the next one
        p.NextLifeStartTicks = p.MissingLives == 0 ? 0 : p.NextLifeStartTicks + lives * perLife;
    }

    private static void GiveLives(UserProfile p, int amount)
    {
        p.MissingLives = Math.Max(0, p.MissingLives - amount);
        if (p.MissingLives == 0) p.NextLifeStartTicks = 0;   // full: no countdown (the next one starts when a life is used)
    }

    private bool Pay(int price)
    {
        if (price <= 0) return true;
        return _wallet != null && _wallet.TrySpendCoins(price);
    }

    private void Commit()
    {
        _save?.Invoke();
        LivesChanged?.Invoke();
    }
}
