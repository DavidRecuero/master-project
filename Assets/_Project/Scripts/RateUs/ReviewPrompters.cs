using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR && PLAY_REVIEW
using Google.Play.Review;
#endif

/// <summary>Picks the review card of the platform the game runs on.</summary>
public static class ReviewPrompters
{
    public static IReviewPrompter Create()
    {
#if UNITY_EDITOR
        return new EditorReviewPrompter();
#elif UNITY_ANDROID && PLAY_REVIEW
        return new GooglePlayReviewPrompter();
#else
        return new UnavailableReviewPrompter();
#endif
    }
}

/// <summary>Editor: no card, only a log line, so the timing can be checked. It counts as an attempt, like on a device.</summary>
public sealed class EditorReviewPrompter : IReviewPrompter
{
    public void Prepare() { }

    public void RequestReview(Action<ReviewRequestResult> onDone)
    {
        Debug.Log("[RateUs] Review card would show now (editor: nothing is shown)");
        onDone?.Invoke(ReviewRequestResult.Launched);
    }

    public void Cancel() { }
}

/// <summary>A build without the Google Play In-app Review package: nothing to show, and no attempt is spent.</summary>
public sealed class UnavailableReviewPrompter : IReviewPrompter
{
    public void Prepare() { }

    public void RequestReview(Action<ReviewRequestResult> onDone)
    {
        Debug.LogWarning("[RateUs] Google Play In-app Review is not in this build: no card.");
        onDone?.Invoke(ReviewRequestResult.Failed);
    }

    public void Cancel() { }
}

#if UNITY_ANDROID && !UNITY_EDITOR && PLAY_REVIEW
/// <summary>
/// The official Google Play review card (Play In-App Review API).
/// Prepare asks Google for a ReviewInfo ahead of time; RequestReview launches the flow with it, so the card
/// opens right away. If it isn't ready yet, the launch waits for it unless Cancel is called first.
/// The card is shown as it comes, on top of everything; the game doesn't touch it.
/// </summary>
public sealed class GooglePlayReviewPrompter : IReviewPrompter
{
    private ReviewManager _manager;
    private PlayReviewInfo _info;
    private bool _preparing;
    private Action<ReviewRequestResult> _waiting;

    public void Prepare()
    {
        if (_info != null || _preparing) return;

        _manager ??= new ReviewManager();
        _preparing = true;

        var request = _manager.RequestReviewFlow();
        request.Completed += op =>
        {
            _preparing = false;
            if (op.Error == ReviewErrorCode.NoError) _info = op.GetResult();
            else Debug.LogWarning($"[RateUs] Review request failed: {op.Error}");

            // Someone is waiting to show it (RequestReview came before the info)
            if (_waiting == null) return;
            Action<ReviewRequestResult> done = _waiting;
            _waiting = null;
            if (_info != null) Launch(done);
            else done(ReviewRequestResult.Failed);
        };
    }

    public void RequestReview(Action<ReviewRequestResult> onDone)
    {
        if (_info != null)
        {
            Launch(onDone);
            return;
        }

        _waiting = onDone;
        Prepare();
    }

    public void Cancel()
    {
        if (_waiting == null) return;

        Action<ReviewRequestResult> done = _waiting;
        _waiting = null;
        Debug.Log("[RateUs] Review card cancelled before it opened");
        done(ReviewRequestResult.Failed);
    }

    private void Launch(Action<ReviewRequestResult> onDone)
    {
        PlayReviewInfo info = _info;
        _info = null;   // one use per ReviewInfo

        var launch = _manager.LaunchReviewFlow(info);
        launch.Completed += op =>
        {
            bool ok = op.Error == ReviewErrorCode.NoError;
            Debug.Log(ok ? "[RateUs] Review flow launched" : $"[RateUs] Review launch failed: {op.Error}");
            onDone?.Invoke(ok ? ReviewRequestResult.Launched : ReviewRequestResult.Failed);
        };
    }
}
#endif
