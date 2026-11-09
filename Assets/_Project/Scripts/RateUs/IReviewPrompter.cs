using System;

public enum ReviewRequestResult
{
    /// <summary>Google accepted the request. It still may not show the card (its own per-account limit), and never says.</summary>
    Launched,
    /// <summary>Google couldn't start it (no Play Store, no connection, plugin missing...), or it was cancelled. Doesn't count as an attempt.</summary>
    Failed
}

/// <summary>Shows the platform's review card. Behind an interface so the timing logic is tested without Google Play.</summary>
public interface IReviewPrompter
{
    /// <summary>
    /// Gets the card ready ahead of time (Google asks to pre-load it, and only once the app is sure it will show it),
    /// so it opens at once when RequestReview is called.
    /// </summary>
    void Prepare();

    /// <summary>Opens the card (preparing it first if needed). onDone is called once.</summary>
    void RequestReview(Action<ReviewRequestResult> onDone);

    /// <summary>
    /// Drops a request that hasn't opened the card yet (still preparing): its onDone gets Failed and the card
    /// never opens. Used when the player leaves the result screen, so the card never shows over gameplay.
    /// </summary>
    void Cancel();
}
