using System;

/// <summary>A clock that only moves when the test says so.</summary>
public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan time) => UtcNow += time;
    public void AdvanceMinutes(double minutes) => UtcNow += TimeSpan.FromMinutes(minutes);
}

/// <summary>Lives with settable values, for the code that only reads or asks for lives.</summary>
public class FakeLivesService : ILivesService
{
    public int Lives { get; set; } = 5;
    public int MaxLives { get; set; } = 5;
    public bool IsFull => Lives >= MaxLives;
    public bool HasLives => Lives > 0;
    public TimeSpan TimeToNextLife { get; set; } = TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(34);
    public int LifePrice { get; set; } = 20;
    public int RefillPrice => (MaxLives - Lives) * LifePrice;
    public int Coins { get; set; } = 100;

    public int UseCalls, ReturnCalls, BuyCalls, RefillCalls;

    public event Action LivesChanged;

    public bool CanAfford(int price) => Coins >= price;

    public bool TryUseLife()
    {
        UseCalls++;
        if (Lives <= 0) return false;
        Lives--;
        LivesChanged?.Invoke();
        return true;
    }

    public void ReturnLife()
    {
        ReturnCalls++;
        if (Lives < MaxLives) Lives++;
        LivesChanged?.Invoke();
    }

    public bool TryBuyLife()
    {
        BuyCalls++;
        if (IsFull || !CanAfford(LifePrice)) return false;
        Coins -= LifePrice;
        Lives++;
        LivesChanged?.Invoke();
        return true;
    }

    public bool TryRefill()
    {
        RefillCalls++;
        int price = RefillPrice;
        if (IsFull || !CanAfford(price)) return false;
        Coins -= price;
        Lives = MaxLives;
        LivesChanged?.Invoke();
        return true;
    }

    public int Listeners => LivesChanged?.GetInvocationList().Length ?? 0;
}
