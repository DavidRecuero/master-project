using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Level scene: asks RateUsService for the review card on the win result screen, never over gameplay.
///   - Level won: the card is got ready in the background (Google asks to pre-load it).
///   - Win popup on screen: the card is asked for (after DelayAfterWinSeconds, 0 by default).
///   - The player leaves the result before the card opened ("Next level", "Menu"): the card is dropped. It
///     doesn't count as an attempt, so it is asked for again on the next win that qualifies.
/// Levels forced with Tools > Level Loader don't count.
/// </summary>
public class RateUsTrigger : MonoBehaviour
{
    [Tooltip("Data/RateUsConfig. Without it the defaults are used: level 8, 3 times, 30 days, no wait.")]
    [SerializeField] private RateUsConfig config;
    [Tooltip("UIManager of the scene: its ResultShown event says when the result popup is on screen")]
    [SerializeField] private UIManager uiManager;

    private RateUsService _service;
    private Func<int> _wonLevel;
    private Coroutine _pending;

    /// <summary>Optional injection (tests). Without it, the real store, card and clock are used.</summary>
    public void Initialize(RateUsService service, Func<int> wonLevel)
    {
        _service = service;
        _wonLevel = wonLevel;
    }

    private void Awake()
    {
        if (_service == null)
        {
            RateUsRules rules = config != null ? config.Rules : RateUsRules.Default;
            _service = new RateUsService(rules, new RateUsStore(new PlayerPrefsStorageProvider()), ReviewPrompters.Create(), new SystemClock());
        }

        if (uiManager == null) Debug.LogWarning("[RateUs] No UIManager assigned to RateUsTrigger: the review card will never be asked for.");

        _wonLevel ??= () =>
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.IsDebugLevel) return 0;   // 0 never asks
            return game.CurrentLevelIndex;
        };
    }

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += OnLevelCleared;
        if (uiManager != null) uiManager.ResultShown += OnResultShown;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= OnLevelCleared;
        if (uiManager != null) uiManager.ResultShown -= OnResultShown;
        LeaveResult();
    }

    /// <summary>The level was won: gets the card ready while the result popup comes in.</summary>
    public void OnLevelCleared()
    {
        if (_service == null || _wonLevel == null) return;
        _service.Prepare(_wonLevel());
    }

    /// <summary>The result popup is on screen. Only wins can ask for the card.</summary>
    public void OnResultShown(bool won)
    {
        if (!won || _service == null || _wonLevel == null) return;

        int level = _wonLevel();
        if (!_service.ShouldAsk(level)) return;

        float delay = _service.Rules.DelayAfterWinSeconds;
        if (delay > 0f && isActiveAndEnabled && Application.isPlaying)
        {
            if (_pending != null) StopCoroutine(_pending);
            _pending = StartCoroutine(PromptAfter(delay, level));
        }
        else
        {
            _service.TryPrompt(level);
        }
    }

    /// <summary>
    /// The result screen is closing (scene change): a card that is waiting or hasn't opened yet is dropped,
    /// so it never shows over the next level. Not counted: the next qualifying win asks again.
    /// </summary>
    public void LeaveResult()
    {
        if (_pending != null) StopCoroutine(_pending);
        _pending = null;

        _service?.Cancel();
    }

    // Real time, so it also works if the game is paused at the end of the level
    private IEnumerator PromptAfter(float seconds, int level)
    {
        yield return new WaitForSecondsRealtime(seconds);
        _pending = null;
        _service.TryPrompt(level);
    }
}
