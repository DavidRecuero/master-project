using System;

/// <summary>
/// Turns gameplay events into vibrations. It only listens to GameEvents and talks to IHapticsService,
/// so gameplay code never knows haptics exist (same shape as GameplayAudioRouter).
/// Not every event vibrates: constant buzzing tires the player and gets haptics turned off, so the
/// strength follows how much the event matters. Item landings, fog steps and coin changes stay silent.
/// Pure C#: the tray state comes in as a function, which keeps it testable in EditMode.
/// </summary>
public class GameplayHapticsRouter
{
    /// <summary>The warning plays once when the free slots drop to this (last slot before losing).</summary>
    public int warningFreeSlots = 1;

    private readonly IHapticsService _haptics;
    private readonly Func<int> _freeTraySlots;

    private int _lastFreeSlots = int.MaxValue;
    private bool _levelEnded;
    private bool _subscribed;

    /// <param name="haptics">Where vibrations go.</param>
    /// <param name="freeTraySlots">Free slots in the tray right now. Null disables the tray warning.</param>
    public GameplayHapticsRouter(IHapticsService haptics, Func<int> freeTraySlots = null)
    {
        _haptics = haptics;
        _freeTraySlots = freeTraySlots;
    }

    public void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;

        GameEvents.OnItemCollectedByPlayer += HandleItemCollected;
        GameEvents.OnTrayChanged += HandleTrayChanged;
        GameEvents.OnMatchCompleted += HandleMatch;
        GameEvents.OnLevelCleared += HandleLevelCleared;
        GameEvents.OnLevelFailed += HandleLevelFailed;
        GameEvents.OnBoosterUsed += HandleBoosterUsed;
        GameEvents.OnBoosterDenied += HandleBoosterDenied;
        GameEvents.OnBarrelDamaged += HandleBarrelDamaged;
    }

    public void Unsubscribe()
    {
        if (!_subscribed) return;
        _subscribed = false;

        GameEvents.OnItemCollectedByPlayer -= HandleItemCollected;
        GameEvents.OnTrayChanged -= HandleTrayChanged;
        GameEvents.OnMatchCompleted -= HandleMatch;
        GameEvents.OnLevelCleared -= HandleLevelCleared;
        GameEvents.OnLevelFailed -= HandleLevelFailed;
        GameEvents.OnBoosterUsed -= HandleBoosterUsed;
        GameEvents.OnBoosterDenied -= HandleBoosterDenied;
        GameEvents.OnBarrelDamaged -= HandleBarrelDamaged;
    }

    // ---------- Handlers ----------

    // Only player taps: the smallest tick, it happens all the time
    private void HandleItemCollected(Item item, Pipe pipe)
    {
        if (_levelEnded) return;
        _haptics?.Play(HapticType.Selection);
    }

    private void HandleTrayChanged()
    {
        if (_levelEnded || _freeTraySlots == null) return;

        int free = _freeTraySlots();
        bool crossedIntoDanger = _lastFreeSlots > warningFreeSlots && free <= warningFreeSlots;
        _lastFreeSlots = free;

        // Only when entering the danger zone, not on every item while in it (it got tiring in play tests).
        // A full tray is the lost level, which has its own pattern.
        if (crossedIntoDanger && free > 0)
        {
            _haptics?.Play(HapticType.Warning);
        }
    }

    private void HandleMatch(int colorId)
    {
        _haptics?.Play(HapticType.MediumImpact);
    }

    private void HandleLevelCleared()
    {
        if (_levelEnded) return;
        _levelEnded = true;
        _haptics?.Play(HapticType.Success);
    }

    private void HandleLevelFailed()
    {
        if (_levelEnded) return;
        _levelEnded = true;
        _haptics?.Play(HapticType.Failure);
    }

    private void HandleBoosterUsed(BoosterType type)
    {
        _haptics?.Play(HapticForBooster(type));
    }

    private void HandleBoosterDenied(BoosterType type)
    {
        _haptics?.Play(HapticType.Warning);
    }

    private void HandleBarrelDamaged(Item barrel, int livesLeft)
    {
        _haptics?.Play(livesLeft <= 0 ? HapticType.HeavyImpact : HapticType.LightImpact);
    }

    /// <summary>The Tray Clearer moves the whole tray, so it hits harder than the other two.</summary>
    public static HapticType HapticForBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Match: return HapticType.MediumImpact;
            case BoosterType.Shuffle: return HapticType.MediumImpact;
            case BoosterType.TrayClearer: return HapticType.HeavyImpact;
            default: return HapticType.None;
        }
    }
}
