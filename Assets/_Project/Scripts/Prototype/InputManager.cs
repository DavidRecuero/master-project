using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public BoardManager boardManager;
    public TrayManager trayManager;

    void Update()
    {
        // Pointer.current handles both Mouse clicks and Touchscreen taps automatically
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            // Get the screen position of the tap/click
            Vector2 screenPosition = Pointer.current.position.ReadValue();

            // Convert the screen position to 2D world coordinates
            Vector2 tapPosition = Camera.main.ScreenToWorldPoint(screenPosition);

            // Cast an invisible ray to see if it hits any Collider
            RaycastHit2D hit = Physics2D.Raycast(tapPosition, Vector2.zero);

            if (hit.collider != null)
            {
                Item clickedItem = hit.collider.GetComponent<Item>();

                if (clickedItem != null)
                    TryCollectItem(clickedItem);
            }
        }
    }

    void TryCollectItem(Item item)
    {
        Pipe parentPipe = item.parentPipe;
        Vector2Int exitPosition = parentPipe.path[parentPipe.path.Count - 1];

        if (item.gridPosition == exitPosition)
        {
            // Try sending item to tray
            if (trayManager.TryAddItem(item))
            {
                // Advance pipe only if item was successfully added to tray
                boardManager.AdvancePipe(parentPipe);
            }
        }
    }
}