using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Event subscribed to by other components
    public static event Action<Item> OnItemClicked;

    private ICameraProvider _cameraProvider;

    public void Initialize(ICameraProvider cameraProvider)
    {
        _cameraProvider = cameraProvider;
    }

    private void Awake()
    {
        if (_cameraProvider == null && Camera.main != null)
        {
            _cameraProvider = new UnityCameraProvider(Camera.main);
        }
    }

    void Update()
    {
        // Pointer.current handles both Mouse clicks and Touchscreen taps automatically
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            // Get the screen position of the tap/click
            Vector2 screenPosition = Pointer.current.position.ReadValue();

            // Convert the screen position to 2D world coordinates
            Vector2 tapPosition = _cameraProvider != null
                ? _cameraProvider.ScreenToWorldPoint(screenPosition)
                : Vector2.zero;

            // Cast an invisible ray to see if it hits any Collider
            RaycastHit2D hit = Physics2D.Raycast(tapPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.TryGetComponent(out Item clickedItem))
            {
                OnItemClicked?.Invoke(clickedItem);
            }
        }
    }
}