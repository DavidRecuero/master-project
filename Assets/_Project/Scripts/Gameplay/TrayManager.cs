using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxCapacity = 7;
    public int matchSize = 3;           //# of items to match them
    public float slotSpacing = 0.8f;
    public float moveSpeed = 15f;
    public float bottomPadding = 1.2f;
    public float snapDistance = 0.01f;
    public float matchDestroyDelay = 0.25f;

    [Header("Sorting Layers")]
    public int slotSortingOrder = -10;
    public int itemInTraySortingOrder = 100;

    [Header("Visuals")]
    public Sprite slotSprite;
    public Color slotColor = new Color(0, 0, 0, 0.3f);
    public float slotScale = 0.95f;

    private List<Item> trayItems = new List<Item>();

    public bool IsFull => trayItems.Count >= maxCapacity;

    [Header("References")]
    public BoardManager boardManager;

    // Injected dependencies
    private IItemPool _itemPool;
    private IGameStateController _gameStateController;
    private ICameraController _cameraController;

    public void Initialize(IItemPool itemPool, IGameStateController gameStateController, ICameraController cameraController)
    {
        _itemPool = itemPool;
        _gameStateController = gameStateController;
        _cameraController = cameraController;
    }

    private void Awake()
    {
        _itemPool ??= ItemPool.Instance;
        _gameStateController ??= GameManager.Instance as IGameStateController;

        if (_cameraController == null && Camera.main != null)
        {
            _cameraController = Camera.main.GetComponent<ICameraController>();
        }
    }

    private void OnEnable()
    {
        InputManager.OnItemClicked += HandleItemClicked;
    }

    private void OnDisable()
    {
        InputManager.OnItemClicked -= HandleItemClicked;
    }

    public void InitializeTray()
    {
        GenerateSlotVisuals();
    }

    // Generates the visual representation of the tray slots
    private void GenerateSlotVisuals()
    {
        // Calculate the starting X based on MAX CAPACITY so the whole tray is centered
        float startX = -((maxCapacity - 1) * slotSpacing) / 2f;
        Vector3 centerPos = GetTrayCenterPosition();

        // Create a visual slot for each potential item in the tray
        for (int i = 0; i < maxCapacity; i++)
        {
            Vector3 slotPos = centerPos + new Vector3(startX + (i * slotSpacing), 0, 0);

            GameObject slotObj = new GameObject($"TraySlot_{i}");
            slotObj.transform.SetParent(this.transform);
            slotObj.transform.position = slotPos;
            slotObj.transform.localScale = new Vector3(slotScale, slotScale, 1f);

            SpriteRenderer sr = slotObj.AddComponent<SpriteRenderer>();
            sr.sprite = slotSprite;
            sr.color = slotColor;
            sr.sortingOrder = slotSortingOrder;
        }
    }

    // Returns the center position of the tray based on the camera's view and bottom padding
    private Vector3 GetTrayCenterPosition()
    {
        float bottomY = _cameraController != null ? _cameraController.GetCameraBottomY() : 0f; 
        return new Vector3(0, bottomY + bottomPadding, 0);
    }

    // Manages the clicks on items
    private void HandleItemClicked(Item item)
    {
        // Avoids processing touches on times already travelling or inside the tray
        if (item.inTray) return;

        Pipe parentPipe = item.parentPipe;
        Vector2Int exitPosition = parentPipe.path[parentPipe.path.Count - 1];

        // Checking if the item is in the last position of the pipe
        if (item.gridPosition == exitPosition)
        {
            if (TryAddItem(item))
            {
                boardManager.AdvancePipe(parentPipe);
            }
        }
    }

    /// <summary>
    /// Attempts to add an item to the tray. Returns true if successful, false if the tray is full.
    /// </summary>
    public bool TryAddItem(Item item)
    {
        if (IsFull) return false;

        Collider2D col = item.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // To stop the tappeable animation
        item.inTray = true;
        item.StopPulse();

        SpriteRenderer itemSr = item.GetComponent<SpriteRenderer>();
        if (itemSr != null) itemSr.sortingOrder = itemInTraySortingOrder;

        item.transform.SetParent(this.transform);

        int lastMatchIndex = trayItems.FindLastIndex(x => x.colorID == item.colorID);
        int insertIndex = (lastMatchIndex != -1) ? lastMatchIndex + 1 : trayItems.Count;

        trayItems.Insert(insertIndex, item);

        UpdateTrayLayout();
        CheckForMatches(item.colorID);

        // Check for Game Over (Tray is full after inserting and matching)
        if (IsFull)
        {
            GameEvents.TriggerLevelFailed();                                                   // Lost
        }

        return true;
    }

    // Updates the positions of all items in the tray to ensure they are evenly spaced and centered
    private void UpdateTrayLayout()
    {
        // Match the visuals: Calculate startX based on maxCapacity, just like the empty slots
        float startX = -((maxCapacity - 1) * slotSpacing) / 2f;
        Vector3 centerPos = GetTrayCenterPosition();

        for (int i = 0; i < trayItems.Count; i++)
        {
            // Items fill from the left side (index 0, 1, 2...)
            Vector3 targetPos = centerPos + new Vector3(startX + (i * slotSpacing), 0, 0);

            trayItems[i].StopAllCoroutines();
            trayItems[i].StartCoroutine(AnimateToPosition(trayItems[i], targetPos));
        }
    }

    // Smoothly animates an item to its target position in the tray
    private IEnumerator AnimateToPosition(Item item, Vector3 targetPos)
    {
        while (item != null && Vector3.Distance(item.transform.position, targetPos) > snapDistance)
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (item != null) item.transform.position = targetPos;
    }

    // Checks for matches of 3 or more items of the same color in the tray and handles their removal
    private void CheckForMatches(int colorIDToCheck)
    {
        List<Item> matchingItems = trayItems.FindAll(x => x.colorID == colorIDToCheck);

        if (matchingItems.Count >= matchSize)
        {
            StartCoroutine(HandleMatch3Visuals(matchingItems));
        }
    }

    // Handles the visual effects and removal of matched items from the tray
    private IEnumerator HandleMatch3Visuals(List<Item> matchedItems)
    {
        foreach (Item item in matchedItems) trayItems.Remove(item);

        yield return new WaitForSeconds(matchDestroyDelay);

        foreach (Item item in matchedItems)
        {
            _itemPool.ReleaseItem(item);
        }

        UpdateTrayLayout();

        // After items dissolve, check if the board and tray are completely cleared
        if (boardManager.IsBoardCleared() && trayItems.Count == 0)
        {
            //WON - we change the state to the victory one
            _gameStateController?.TrySetState(GameState.Victory);
        }
    }
}