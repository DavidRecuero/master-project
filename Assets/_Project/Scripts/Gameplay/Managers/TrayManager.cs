using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxCapacity = 7;
    public int matchSize = 3;           //# of items to match them
    [Tooltip("Distance between slot centres (world units). 0.944 = 46 pt slot + 5 pt gap, with a cell of 54 pt = 1 unit")]
    public float slotSpacing = 0.944f;
    public float moveSpeed = 15f;
    public float bottomPadding = 1.2f;
    public float snapDistance = 0.01f;
    public float matchDestroyDelay = 0.25f;

    [Header("Sorting Layers")]
    public int slotSortingOrder = -10;
    public int itemInTraySortingOrder = 100;

    [Header("Visuals")]
    public Sprite slotSprite;
    public Color slotColor = Color.white;
    [Tooltip("1 with Tray_Slot (46 pt drawn on a 256 px cell)")]
    public float slotScale = 1f;
    [Tooltip("Visible diameter of the slot drawing at slotScale 1 (world units). Tray_Slot: 47 pt of a 54 pt cell = 0.870")]
    public float slotVisualSize = 0.870f;

    [Header("Tray Panel")]
    [Tooltip("9-slice panel behind the slots (Board_Panel, same style as the board). Empty = no panel.")]
    public Sprite panelSprite;
    [Tooltip("Space between the slots and the panel edge (world units): x = sides, y = top and bottom")]
    public Vector2 panelPadding = new Vector2(0.185f, 0.148f);
    [Tooltip("Height of the shadow baked under the panel sprite (world units)")]
    public float panelShadowHeight = 0.111f;
    public int panelSortingOrder = -12;

    [Header("Match Feedback")]
    [Tooltip("Matched items grow and fade during matchDestroyDelay before leaving")]
    public bool popOnMatch = true;
    public float popScale = 1.3f;

    private List<Item> trayItems = new List<Item>();
    private readonly List<GameObject> trayVisuals = new List<GameObject>();

    public bool IsFull => trayItems.Count >= maxCapacity;
    public IReadOnlyList<Item> TrayItems => trayItems;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;

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
        boardManager ??= FindFirstObjectByType<BoardManager>();
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

    // Generates the visual representation of the tray: the panel and one slot per capacity
    private void GenerateSlotVisuals()
    {
        ClearSlotVisuals();

        // Calculate the starting X based on MAX CAPACITY so the whole tray is centered
        float startX = -((maxCapacity - 1) * slotSpacing) / 2f;
        Vector3 centerPos = GetTrayCenterPosition();

        CreatePanel(centerPos);

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
            trayVisuals.Add(slotObj);
        }
    }

    /// <summary>Size of the panel face (without its shadow) that wraps every slot.</summary>
    public Vector2 PanelFaceSize()
    {
        float slotSize = SlotWorldSize();
        float width = (maxCapacity - 1) * slotSpacing + slotSize + panelPadding.x * 2f;
        float height = slotSize + panelPadding.y * 2f;
        return new Vector2(width, height);
    }

    // Visible diameter of one slot in the world
    private float SlotWorldSize()
    {
        return slotVisualSize * slotScale;
    }

    private void CreatePanel(Vector3 centerPos)
    {
        if (panelSprite == null) return;

        Vector2 face = PanelFaceSize();

        GameObject panelObj = new GameObject("TrayPanel");
        panelObj.transform.SetParent(transform);
        // The sprite carries its shadow below the face, so the whole sprite sits half a shadow lower
        panelObj.transform.position = centerPos + new Vector3(0f, -panelShadowHeight * 0.5f, 0f);

        SpriteRenderer sr = panelObj.AddComponent<SpriteRenderer>();
        sr.sprite = panelSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(face.x, face.y + panelShadowHeight);
        sr.sortingOrder = panelSortingOrder;
        trayVisuals.Add(panelObj);
    }

    // Removes the panel and slots of a previous level, so they never pile up
    private void ClearSlotVisuals()
    {
        foreach (GameObject go in trayVisuals)
        {
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        trayVisuals.Clear();
    }

    /// <summary>
    /// Removes a specific item currently sitting in the tray (without matching it),
    /// releasing it back to the pool and re-laying out the remaining items.
    /// Used by the TrayClearer booster.
    /// </summary>
    public void RemoveItemFromTray(Item item)
    {
        if (item == null || !trayItems.Remove(item)) return;

        item.StopAllCoroutines();
        _itemPool.ReleaseItem(item);

        UpdateTrayLayout();
        GameEvents.TriggerTrayChanged();
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
        // Capture the pipe before collecting, the item leaves it during CollectItem
        Pipe sourcePipe = item != null ? item.parentPipe : null;

        if (CollectItem(item))
        {
            // Only player taps notify obstacles (fog movement, barrel damage)
            GameEvents.TriggerItemCollectedByPlayer(item, sourcePipe);
        }
    }

    /// <summary>
    /// Attempts to send a pipe item into the tray, exactly as if the player had tapped it.
    /// Returns false if the item isn't currently pickable (not at its pipe's exit) or the tray is full.
    /// Reused by the Match booster to auto-collect items.
    /// </summary>
    public bool CollectItem(Item item)
    {
        if (item == null || !item.CanBeCollected) return false;

        Pipe parentPipe = item.parentPipe;

        if (!TryAddItem(item)) return false;

        boardManager.AdvancePipe(parentPipe);
        return true;
    }

    /// <summary>
    /// Attempts to add an item to the tray. Returns true if successful, false if the tray is full.
    /// </summary>
    public bool TryAddItem(Item item)
    {
        if (IsFull) return false;

        // Hidden items show their real color as soon as they land in the tray, before grouping and matching (both rely on colorID)
        item.Reveal();

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

        GameEvents.TriggerTrayChanged();

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

        if (popOnMatch) yield return PopItems(matchedItems, matchDestroyDelay);
        else yield return new WaitForSeconds(matchDestroyDelay);

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

    // Matched items swell and fade out. Item.Init restores scale and colour when the pool reuses them.
    private IEnumerator PopItems(List<Item> items, float duration)
    {
        var renderers = new List<SpriteRenderer>();
        var startScales = new List<float>();
        foreach (Item item in items)
        {
            renderers.Add(item != null ? item.GetComponent<SpriteRenderer>() : null);
            startScales.Add(item != null ? item.transform.localScale.x : 1f);
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float grow = Mathf.Sin(k * Mathf.PI * 0.5f);   // Fast start, soft end

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                float s = Mathf.Lerp(startScales[i], startScales[i] * popScale, grow);
                items[i].transform.localScale = new Vector3(s, s, 1f);

                if (renderers[i] != null)
                {
                    Color c = renderers[i].color;
                    c.a = 1f - k * k;
                    renderers[i].color = c;
                }
            }
            yield return null;
        }
    }
}