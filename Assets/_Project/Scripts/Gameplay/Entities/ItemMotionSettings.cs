using UnityEngine;

/// <summary>
/// Timings and amounts of the item animations: the chain that moves along the pipe, the arrival on the
/// exit platform and the flight to the tray. Designer-tunable, like ItemPalette and PipeTileSet.
/// Only the visuals use it: the game logic (grid positions, tray contents) never waits for an animation.
/// </summary>
[CreateAssetMenu(fileName = "ItemMotionSettings", menuName = "Game/Item Motion Settings")]
public class ItemMotionSettings : ScriptableObject
{
    [Header("Along the pipe (chain)")]
    [Tooltip("Seconds an item takes to slide one cell")]
    public float stepDuration = 0.17f;
    [Tooltip("Seconds between the start of the first item (next to the exit) and the next one behind it: the wave that runs up the pipe")]
    public float stepStagger = 0.045f;
    [Tooltip("Seconds between the tap and the first item starting to move")]
    public float stepStartDelay = 0.06f;
    [Tooltip("How much the item stretches while it slides (0.08 = 8 %)")]
    public float stepStretch = 0.08f;
    [Tooltip("How much it squashes when it stops in its new cell")]
    public float stepLandSquash = 0.12f;
    [Tooltip("Seconds of that squash")]
    public float stepLandDuration = 0.14f;
    [Tooltip("Taps faster than the animation: an item with more than one cell still to go takes this part of a step for each extra cell (1 = a full step per cell, 0 = always one step). Lower catches up faster")]
    [Range(0f, 1f)] public float chainCatchUp = 0.6f;
    [Tooltip("New items come in from this far behind the first cell, in cells (0.5 = the inlet mouth)")]
    public float inletOffset = 0.5f;

    [Header("Arriving on the exit platform")]
    [Tooltip("Seconds it takes to grow to the platform size")]
    public float growDuration = 0.26f;
    [Tooltip("When the growth starts, as a part of the last slide (0.55 = a bit after half way)")]
    [Range(0f, 1f)] public float growStartAt = 0.55f;
    [Tooltip("Overshoot of the growth (0 = none, 1.7 = a clear bounce)")]
    public float growOvershoot = 1.9f;

    [Header("Flight to the tray")]
    [Tooltip("Seconds from the platform (or anywhere on the board, Match booster) to the tray slot")]
    public float flyDuration = 0.42f;
    [Tooltip("Height of the arc above the straight line, in world units (1 = one cell)")]
    public float flyArcHeight = 1.25f;
    [Tooltip("Extra size in the middle of the flight (0.14 = 14 % bigger at the top of the arc)")]
    public float flyScaleBoost = 0.14f;
    [Tooltip("Stretch while flying")]
    public float flyStretch = 0.10f;
    [Tooltip("Tilt in the middle of the flight, in degrees (towards where it is going)")]
    public float flyTilt = 12f;
    [Tooltip("Squash when it lands in the slot")]
    public float trayLandSquash = 0.21f;
    [Tooltip("Seconds of that squash")]
    public float trayLandDuration = 0.16f;

    [Header("Inside the tray")]
    [Tooltip("Seconds an item takes to slide to another slot when the tray reorders (an item goes in between, a match leaves a gap)")]
    public float traySlideDuration = 0.18f;
    [Tooltip("Max seconds a match waits for its last item to land before popping")]
    public float matchMaxWait = 1f;

    private static ItemMotionSettings _defaults;

    /// <summary>The values above, for items that were never given an asset (tests, a scene without it).</summary>
    public static ItemMotionSettings Defaults
    {
        get
        {
            if (_defaults == null)
            {
                _defaults = CreateInstance<ItemMotionSettings>();
                _defaults.hideFlags = HideFlags.HideAndDontSave;
            }
            return _defaults;
        }
    }

    /// <summary>When the item at chainIndex (0 = the first one to move) starts sliding after a tap.</summary>
    public float StepDelay(int chainIndex) => stepStartDelay + Mathf.Max(0, chainIndex) * stepStagger;
}
