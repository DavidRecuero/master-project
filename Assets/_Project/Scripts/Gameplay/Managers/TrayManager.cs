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
    public float matchDestroyDelay = 0.25f;

    [Header("Animation")]
    [Tooltip("Flight to the tray, landing and sliding between slots. Use the same asset as the BoardManager. Empty: the defaults")]
    public ItemMotionSettings itemMotion;

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

    [Header("Placement")]
    [Tooltip("Gap between the bottom of the play area (top of the booster band) and the tray panel's shadow (world units). 0.2 = ~11 pt")]
    public float gapAboveBand = 0.2f;

    [Header("Match Feedback")]
    [Tooltip("Matched items grow and fade during matchDestroyDelay before leaving")]
    public bool popOnMatch = true;
    public float popScale = 1.3f;

    private List<Item> trayItems = new List<Item>();
    // Slot each tray item is drawn in (from the last layout)
    private readonly Dictionary<Item, int> slotOf = new Dictionary<Item, int>();
    // Matched items still popping, and the slot they keep. A match frees its slots at once for the logic
    // (trayItems), but on screen they stay taken until the pop ends, so an item arriving meanwhile
    // never lands on top of a popping one, and the gap only closes when they are gone.
    private readonly Dictionary<Item, int> poppingSlots = new Dictionary<Item, int>();
    private readonly List<GameObject> trayVisuals = new List<GameObject>();
    // Items flying into the tray that haven't reached their slot yet (they announce the landing once)
    private readonly HashSet<Item> arrivingItems = new HashSet<Item>();

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
        if (boardManager == null) boardManager = FindFirstObjectByType<BoardManager>();
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

        slotOf.Remove(item);
        arrivingItems.Remove(item);
        _itemPool.ReleaseItem(item);

        UpdateTrayLayout();
        GameEvents.TriggerTrayChanged();
    }

    // The tray rests on the bottom of the play area the camera reports (right above the booster band)
    private Vector3 GetTrayCenterPosition()
    {
        float playAreaBottom = _cameraController != null ? _cameraController.GetPlayAreaBottomY() : 0f;
        return new Vector3(0, CenterYAboveBand(playAreaBottom), 0);
    }

    /// <summary>Slot row Y so the panel (face + its shadow) rests gapAboveBand over the band's top edge.</summary>
    public float CenterYAboveBand(float bandTopY)
    {
        return bandTopY + gapAboveBand + panelShadowHeight + PanelFaceSize().y / 2f;
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

        float startScale = PrepareForTray(item);

        int lastMatchIndex = trayItems.FindLastIndex(x => x.colorID == item.colorID);
        int insertIndex = (lastMatchIndex != -1) ? lastMatchIndex + 1 : trayItems.Count;

        trayItems.Insert(insertIndex, item);
        arrivingItems.Add(item);

        // The new item flies in an arc to its slot; the ones it pushes aside slide over
        Dictionary<Item, int> layout = ComputeLayout();
        item.Motion.FlyTo(SlotPosition(layout[item]), startScale, item.defaultScale, () => OnItemLanded(item));
        UpdateTrayLayout(layout);
        CheckForMatches(item.colorID);

        GameEvents.TriggerTrayChanged();

        // Check for Game Over (Tray is full after inserting and matching)
        if (IsFull)
        {
            GameEvents.TriggerLevelFailed();                                                   // Lost
        }

        return true;
    }

    /// <summary>
    /// Adds items that complete a match together, all at once (Match booster with an almost full tray).
    /// One by one, the first piece could fill the tray and lose the level before the next one completes the trio.
    /// Here the pieces fly to the slot of their color, the whole trio is resolved at once, and only then is the tray
    /// checked for a defeat. It never goes over the real capacity: the trio leaves as soon as it is formed.
    /// False (nothing added) if the items are not all the same color or don't complete a match with the tray.
    /// </summary>
    public bool TryAddMatchingGroup(IReadOnlyList<Item> items)
    {
        if (items == null || items.Count == 0 || items[0] == null) return false;

        int color = items[0].colorID;
        foreach (Item item in items)
        {
            if (item == null || item.colorID != color) return false;
        }

        int inTray = trayItems.FindAll(x => x.colorID == color).Count;
        if (inTray == 0 || inTray + items.Count < matchSize) return false;

        // The slot of the last piece of that color: the new ones gather there and pop with it
        Dictionary<Item, int> before = ComputeLayout();
        Item anchor = trayItems.FindLast(x => x.colorID == color);
        int anchorSlot = before.TryGetValue(anchor, out int slot) ? slot : trayItems.Count - 1;

        foreach (Item item in items)
        {
            float startScale = PrepareForTray(item);

            int lastMatchIndex = trayItems.FindLastIndex(x => x.colorID == color);
            trayItems.Insert(lastMatchIndex + 1, item);
            arrivingItems.Add(item);
            slotOf[item] = anchorSlot;

            item.Motion.FlyTo(SlotPosition(anchorSlot), startScale, item.defaultScale, () => OnItemLanded(item));
        }

        // The whole trio leaves the tray now (it pops when the last piece lands); the rest slide into place
        CheckForMatches(color);
        UpdateTrayLayout();

        GameEvents.TriggerTrayChanged();

        if (IsFull) GameEvents.TriggerLevelFailed();

        return true;
    }

    /// <summary>
    /// Turns a board item into a tray item: real color shown, no taps, tray look, child of the tray.
    /// Returns the size it leaves the board with, where its flight starts.
    /// </summary>
    private float PrepareForTray(Item item)
    {
        // Hidden items show their real color as soon as they land in the tray, before grouping and matching (both rely on colorID)
        bool wasHidden = item.IsHidden;
        item.Reveal();
        if (wasHidden) GameEvents.TriggerHiddenItemRevealed(item);

        Collider2D col = item.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Size it leaves the board with (platform size), before it becomes a tray item
        float startScale = item.CurrentVisualScale;

        // To stop the tappeable animation
        item.inTray = true;
        item.StopPulse();
        if (itemMotion != null) item.SetMotionSettings(itemMotion);

        SpriteRenderer itemSr = item.GetComponent<SpriteRenderer>();
        if (itemSr != null) itemSr.sortingOrder = itemInTraySortingOrder;

        item.transform.SetParent(this.transform);
        return startScale;
    }

    /// <summary>World position of a tray slot (slots fill from the left: 0, 1, 2...).</summary>
    public Vector3 SlotPosition(int index)
    {
        // Match the visuals: startX comes from maxCapacity, just like the empty slots
        float startX = -((maxCapacity - 1) * slotSpacing) / 2f;
        return GetTrayCenterPosition() + new Vector3(startX + (index * slotSpacing), 0, 0);
    }

    /// <summary>
    /// Slot of every tray item, in tray order, skipping the slots of the items still popping.
    /// If they don't all fit (very fast taps right after a match), the popping slots are ignored.
    /// </summary>
    private Dictionary<Item, int> ComputeLayout()
    {
        var layout = new Dictionary<Item, int>();
        // Distinct slots only: the pieces a group match brings share the slot of their color
        var taken = new HashSet<int>();
        foreach (int popping in poppingSlots.Values)
        {
            if (popping >= 0) taken.Add(popping);
        }
        bool keepPoppingSlots = trayItems.Count + taken.Count <= maxCapacity;

        int slot = 0;
        foreach (Item item in trayItems)
        {
            while (keepPoppingSlots && taken.Contains(slot)) slot++;
            layout[item] = slot++;
        }
        return layout;
    }

    // Sends every tray item to its slot: the ones still flying just change their landing point, the rest slide.
    // Popping items stay where they are.
    private void UpdateTrayLayout(Dictionary<Item, int> layout = null)
    {
        float slide = (itemMotion != null ? itemMotion : ItemMotionSettings.Defaults).traySlideDuration;
        layout ??= ComputeLayout();

        foreach (KeyValuePair<Item, int> entry in layout)
        {
            Item item = entry.Key;
            if (item == null) continue;

            slotOf[item] = entry.Value;
            Vector3 target = SlotPosition(entry.Value);
            ItemMotion motion = item.Motion;

            if (arrivingItems.Contains(item) && motion.IsMoving) motion.Retarget(target);
            else if ((item.transform.position - target).sqrMagnitude > 0.0001f) motion.SlideTo(target, 0f, slide, false, 0f, 0f);
        }
    }

    // The flight reached its slot: the landing sound, once per item (not for an item that already matched)
    private void OnItemLanded(Item item)
    {
        if (arrivingItems.Remove(item)) GameEvents.TriggerItemLandedInTray(item);
    }

    // Checks for matches of 3 or more items of the same color in the tray and handles their removal
    private void CheckForMatches(int colorIDToCheck)
    {
        List<Item> matchingItems = trayItems.FindAll(x => x.colorID == colorIDToCheck);

        if (matchingItems.Count >= matchSize)
        {
            StartCoroutine(HandleMatch3Visuals(matchingItems, colorIDToCheck));
        }
    }

    // Handles the visual effects and removal of matched items from the tray
    private IEnumerator HandleMatch3Visuals(List<Item> matchedItems, int colorId)
    {
        // The tray contents change at once (the next tap already sees the free slots)
        foreach (Item item in matchedItems)
        {
            trayItems.Remove(item);
            poppingSlots[item] = slotOf.TryGetValue(item, out int slot) ? slot : -1;   // keeps its slot on screen until the pop ends
            slotOf.Remove(item);
            arrivingItems.Remove(item);   // The match sound covers it, no landing sound on top
        }

        // The pop (and its sound) waits for the last item to land in its slot
        float maxWait = (itemMotion != null ? itemMotion : ItemMotionSettings.Defaults).matchMaxWait;
        float waited = 0f;
        while (waited < maxWait && matchedItems.Exists(i => i != null && i.Motion.IsMoving))
        {
            waited += Time.deltaTime;
            yield return null;
        }

        GameEvents.TriggerMatchCompleted(colorId);

        // From here the pop scales the items itself
        foreach (Item item in matchedItems)
        {
            if (item != null) item.Motion.enabled = false;
        }

        if (popOnMatch) yield return PopItems(matchedItems, matchDestroyDelay);
        else yield return new WaitForSeconds(matchDestroyDelay);

        foreach (Item item in matchedItems)
        {
            poppingSlots.Remove(item);
            _itemPool.ReleaseItem(item);
        }

        // Now the gap closes
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