using System;
using UnityEngine;

/// <summary>
/// The door to a level: every "play" (main menu Play, Retry after losing) goes through here.
/// With a life, the level starts. Without one, the "out of lives" popup opens instead; it starts the level
/// if the player buys a life there (or one comes back while it is open).
/// Without a lives system (scene opened without the Boot) it lets the player in, so testing a scene
/// on its own still works.
/// </summary>
public sealed class LivesGate
{
    private readonly ILivesService _lives;
    private readonly IPopupService _popups;
    private readonly NoLivesPopup _noLivesPopupPrefab;

    public LivesGate(ILivesService lives, IPopupService popups, NoLivesPopup noLivesPopupPrefab)
    {
        _lives = lives;
        _popups = popups;
        _noLivesPopupPrefab = noLivesPopupPrefab;
    }

    /// <summary>True if the level started now; false if the popup opened instead (or nothing could be done).</summary>
    public bool TryEnter(Action enterLevel)
    {
        if (enterLevel == null) return false;

        if (_lives == null || _lives.HasLives)
        {
            enterLevel();
            return true;
        }

        if (_popups == null || _noLivesPopupPrefab == null)
        {
            Debug.LogWarning("[Lives] No lives left and no NoLivesPopup to show. Start from the Boot scene and assign the prefab.");
            return false;
        }

        NoLivesPopup popup = _popups.Open(_noLivesPopupPrefab);
        if (popup != null) popup.Initialize(_lives, enterLevel);
        return false;
    }
}
