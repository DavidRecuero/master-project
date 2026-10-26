using System;

/// <summary>
/// Turns gameplay events into sound effects. It only listens to GameEvents and talks to IAudioService,
/// so gameplay code never knows audio exists. Pure C#: the time and the tray state come in as functions,
/// which keeps it testable in EditMode.
/// </summary>
public class GameplayAudioRouter
{
    /// <summary>Free tray slots at or below this play the warning (when the tray just got fuller).</summary>
    public int warningFreeSlots = 2;
    /// <summary>Seconds the music stays ducked under the win/lose jingle.</summary>
    public float jingleDuckSeconds = 3f;

    private readonly IAudioService _audio;
    private readonly Func<float> _now;
    private readonly Func<int> _freeTraySlots;
    private readonly ComboTracker _combo;

    private int _lastFreeSlots = int.MaxValue;
    private int _lastCoins;
    private bool _levelEnded;
    private bool _subscribed;

    /// <param name="audio">Where sounds go.</param>
    /// <param name="now">Current time in seconds (Time.unscaledTime in the game).</param>
    /// <param name="freeTraySlots">Free slots in the tray right now. Null disables the tray warning.</param>
    /// <param name="startingCoins">Coin balance when the level starts, to tell gains from spends.</param>
    public GameplayAudioRouter(IAudioService audio, Func<float> now, Func<int> freeTraySlots = null,
                               int startingCoins = 0, ComboTracker combo = null)
    {
        _audio = audio;
        _now = now ?? (() => 0f);
        _freeTraySlots = freeTraySlots;
        _lastCoins = startingCoins;
        _combo = combo ?? new ComboTracker();
    }

    public void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;

        GameEvents.OnItemCollectedByPlayer += HandleItemCollected;
        GameEvents.OnItemLandedInTray += HandleItemLanded;
        GameEvents.OnTrayChanged += HandleTrayChanged;
        GameEvents.OnMatchCompleted += HandleMatch;
        GameEvents.OnLevelCleared += HandleLevelCleared;
        GameEvents.OnLevelFailed += HandleLevelFailed;
        GameEvents.OnBoosterUsed += HandleBoosterUsed;
        GameEvents.OnBoosterDenied += HandleBoosterDenied;
        GameEvents.OnBarrelDamaged += HandleBarrelDamaged;
        GameEvents.OnFogMoved += HandleFogMoved;
        GameEvents.OnHiddenItemRevealed += HandleHiddenRevealed;
        GameEvents.OnCoinsChanged += HandleCoinsChanged;
    }

    public void Unsubscribe()
    {
        if (!_subscribed) return;
        _subscribed = false;

        GameEvents.OnItemCollectedByPlayer -= HandleItemCollected;
        GameEvents.OnItemLandedInTray -= HandleItemLanded;
        GameEvents.OnTrayChanged -= HandleTrayChanged;
        GameEvents.OnMatchCompleted -= HandleMatch;
        GameEvents.OnLevelCleared -= HandleLevelCleared;
        GameEvents.OnLevelFailed -= HandleLevelFailed;
        GameEvents.OnBoosterUsed -= HandleBoosterUsed;
        GameEvents.OnBoosterDenied -= HandleBoosterDenied;
        GameEvents.OnBarrelDamaged -= HandleBarrelDamaged;
        GameEvents.OnFogMoved -= HandleFogMoved;
        GameEvents.OnHiddenItemRevealed -= HandleHiddenRevealed;
        GameEvents.OnCoinsChanged -= HandleCoinsChanged;
    }

    // ---------- Handlers ----------

    // Only player taps: the Match booster collects items too, but it has its own sound
    private void HandleItemCollected(Item item, Pipe pipe)
    {
        if (_levelEnded) return;
        _audio?.PlaySfx(SfxId.ItemTap);
    }

    // Any item reaching its tray slot, including the ones the Match booster sends
    private void HandleItemLanded(Item item)
    {
        _audio?.PlaySfx(SfxId.ItemLand);
    }

    private void HandleTrayChanged()
    {
        if (_levelEnded || _freeTraySlots == null) return;

        int free = _freeTraySlots();
        bool gotFuller = free < _lastFreeSlots;
        _lastFreeSlots = free;

        // Full tray means the level is lost: that has its own sound
        if (gotFuller && free > 0 && free <= warningFreeSlots)
        {
            _audio?.PlaySfx(SfxId.TrayWarning);
        }
    }

    private void HandleMatch(int colorId)
    {
        float pitch = _combo.RegisterMatch(_now());
        _audio?.PlaySfx(SfxId.Match3, pitch);
    }

    private void HandleLevelCleared()
    {
        if (_levelEnded) return;
        _levelEnded = true;
        _audio?.DuckMusic(jingleDuckSeconds);
        _audio?.PlaySfx(SfxId.LevelWin);
    }

    private void HandleLevelFailed()
    {
        if (_levelEnded) return;
        _levelEnded = true;
        _audio?.DuckMusic(jingleDuckSeconds);
        _audio?.PlaySfx(SfxId.LevelLose);
    }

    private void HandleBoosterUsed(BoosterType type)
    {
        _audio?.PlaySfx(SfxForBooster(type));
    }

    private void HandleBoosterDenied(BoosterType type)
    {
        _audio?.PlaySfx(SfxId.BoosterDenied);
    }

    private void HandleBarrelDamaged(Item barrel, int livesLeft)
    {
        _audio?.PlaySfx(livesLeft <= 0 ? SfxId.BarrelBreak : SfxId.BarrelHit);
    }

    private void HandleFogMoved()
    {
        if (_levelEnded) return;
        _audio?.PlaySfx(SfxId.FogMove);
    }

    private void HandleHiddenRevealed(Item item)
    {
        _audio?.PlaySfx(SfxId.HiddenReveal);
    }

    private void HandleCoinsChanged(int balance)
    {
        if (balance > _lastCoins) _audio?.PlaySfx(SfxId.CoinsGain);
        else if (balance < _lastCoins) _audio?.PlaySfx(SfxId.CoinsSpend);
        _lastCoins = balance;
    }

    public static SfxId SfxForBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Match: return SfxId.BoosterMatch;
            case BoosterType.Shuffle: return SfxId.BoosterShuffle;
            case BoosterType.TrayClearer: return SfxId.BoosterTrayClear;
            default: return SfxId.None;
        }
    }
}
