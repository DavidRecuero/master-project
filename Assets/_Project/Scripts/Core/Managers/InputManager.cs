using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Event subscribed to by other components
    public static event Action<Item> OnItemClicked;

    private ICameraProvider _cameraProvider;

    // Board taps are ignored once the level has ended (the result popup is up)
    private bool _levelEnded;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

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

    private void OnEnable()
    {
        GameEvents.OnLevelCleared += HandleLevelEnded;
        GameEvents.OnLevelFailed += HandleLevelEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCleared -= HandleLevelEnded;
        GameEvents.OnLevelFailed -= HandleLevelEnded;
    }

    private void HandleLevelEnded() => _levelEnded = true;

    void Update()
    {
        if (_levelEnded) return;

        // Pointer.current handles both Mouse clicks and Touchscreen taps automatically
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            // Get the screen position of the tap/click
            Vector2 screenPosition = Pointer.current.position.ReadValue();

            // A tap on the UI (boosters, band, popup...) never reaches the board behind it
            if (IsOverUi(screenPosition)) return;

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

    // Asks the UI raycasters directly, so it works the same with mouse and touch and doesn't depend on update order
    private bool IsOverUi(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var pointer = new PointerEventData(eventSystem) { position = screenPosition };
        _uiHits.Clear();
        eventSystem.RaycastAll(pointer, _uiHits);
        return _uiHits.Count > 0;
    }
}