using System;
using UnityEngine;

/// <summary>
/// Asks for the review card after a win when RateUsPolicy says it's the moment, and remembers the attempt
/// once Google accepted it. Pure C#: the rules, the storage, the platform card and the clock come from outside.
/// </summary>
public sealed class RateUsService
{
    private readonly RateUsRules _rules;
    private readonly RateUsStore _store;
    private readonly IReviewPrompter _prompter;
    private readonly IClock _clock;
    private bool _requesting;

    public RateUsService(RateUsRules rules, RateUsStore store, IReviewPrompter prompter, IClock clock)
    {
        _rules = rules;
        _store = store;
        _prompter = prompter;
        _clock = clock ?? new SystemClock();
    }

    public RateUsRules Rules => _rules;

    /// <summary>True if winning this level should ask for the card now.</summary>
    public bool ShouldAsk(int wonLevel) =>
        !_requesting && _store != null && _prompter != null &&
        RateUsPolicy.ShouldAsk(wonLevel, _store.Prompts, _store.LastPromptUtc, _clock.UtcNow, _rules);

    /// <summary>The level was just won: if the card is going to be asked for, gets it ready so it opens at once.</summary>
    public void Prepare(int wonLevel)
    {
        if (ShouldAsk(wonLevel)) _prompter.Prepare();
    }

    /// <summary>The player left the result screen: a card that hasn't opened yet is dropped (not counted).</summary>
    public void Cancel()
    {
        if (_requesting) _prompter?.Cancel();
    }

    /// <summary>Asks for the card if it's the moment. True if it was asked for.</summary>
    public bool TryPrompt(int wonLevel)
    {
        if (!ShouldAsk(wonLevel)) return false;

        _requesting = true;
        Debug.Log($"[RateUs] Asking for the review card (level {wonLevel}, attempt {_store.Prompts + 1}/{_rules.MaxPrompts})");

        _prompter.RequestReview(result =>
        {
            _requesting = false;
            if (result == ReviewRequestResult.Launched) _store.RecordPrompt(_clock.UtcNow);
        });
        return true;
    }
}
