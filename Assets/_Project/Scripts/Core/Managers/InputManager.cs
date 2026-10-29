using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Board input: the single gate between the player's taps on items and the gameplay.
///
/// Taps reach the items through the EventSystem, the same path as the UI: a Physics2DRaycaster on the
/// camera finds the item under the pointer and the item raises Item.Pressed. This replaces polling
/// Pointer.current.press, which only reads the last used pointer: in the Device Simulator (and on a device
/// after losing focus mid-touch) it could get stuck on a pointer that no longer reports presses, so taps on
/// the board stopped while the UI kept working.
///
/// The EventSystem also sorts every raycaster: a screen-space UI element over the board (boosters, band,
/// popups) takes the tap first, so the board behind it never gets it.
/// </summary>
public class InputManager : MonoBehaviour
{
    // Event subscribed to by other components
    public static event Action<Item> OnItemClicked;

    // Board taps are ignored once the level has ended (the result popup is up)
    private bool _levelEnded;

    private void Awake()
    {
        EnsurePhysicsRaycaster(Camera.main);
    }

    private void OnEnable()
    {
        Item.Pressed += HandleItemPressed;
        GameEvents.OnLevelCleared += HandleLevelEnded;
        GameEvents.OnLevelFailed += HandleLevelEnded;
    }

    private void OnDisable()
    {
        Item.Pressed -= HandleItemPressed;
        GameEvents.OnLevelCleared -= HandleLevelEnded;
        GameEvents.OnLevelFailed -= HandleLevelEnded;
    }

    private void HandleLevelEnded() => _levelEnded = true;

    private void HandleItemPressed(Item item)
    {
        if (_levelEnded || item == null) return;
        OnItemClicked?.Invoke(item);
    }

    /// <summary>
    /// Without a Physics2DRaycaster on the camera the EventSystem never sees the items' colliders.
    /// It should be in the scene; this only adds it (and warns) if it was forgotten.
    /// </summary>
    public static Physics2DRaycaster EnsurePhysicsRaycaster(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogWarning("[InputManager] No main camera: items can't be tapped.");
            return null;
        }

        if (camera.TryGetComponent(out Physics2DRaycaster raycaster)) return raycaster;

        Debug.LogWarning("[InputManager] The camera had no Physics2DRaycaster. Added at runtime: add it to the scene.");
        return camera.gameObject.AddComponent<Physics2DRaycaster>();
    }
}
