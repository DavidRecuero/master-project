using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Coordinate grid only (cell size and origin). Nothing is painted on it.")]
    public Tilemap tilemap;
    [SerializeField] private TrayManager trayManager;

    [Header("Level Data")]
    private int currentLevelNumber;
    public int CurrentLevelNumber => currentLevelNumber;

    [Header("Board Data")]
    private int width;
    private int height;
    private List<Pipe> pipes = new List<Pipe>();
    [Tooltip("Space between the top of the booster band and the board's last row: tray + gaps + panel bottom (world units)")]
    public float traySpace = 2.0f;

    public int Width => width;
    public int Height => height;
    public IReadOnlyList<Pipe> Pipes => pipes;

    [Header("Pipe Visuals")]
    [Tooltip("Draws the board: tubes, mouths, platforms, inlet counters and the board panel")]
    public PipeBoardRenderer pipeBoardRenderer;

    [Header("Item Visuals")]
    [Tooltip("Finished sprite and color for each colorId, plus the hidden sprite")]
    public ItemPalette itemPalette;

    [Tooltip("Timings of the item animations (pipe, platform, tray). Empty: the defaults")]
    public ItemMotionSettings itemMotion;

    [Header("Barrel Settings")]
    public int defaultBarrelLives = 3;
    [Tooltip("Crate sprites: [0] = intact, each next one more damaged")]
    public Sprite[] barrelDamageSprites;

    //Injected dependencies
    private ILevelLoader _levelLoader;
    private IItemPool _itemPool;
    private ICameraController _cameraController;

    private void Awake()
    {
        if (trayManager == null) trayManager = FindFirstObjectByType<TrayManager>();
    }

    public void Initialize(ILevelLoader levelLoader, IItemPool itemPool, ICameraController cameraController)
    {
        _levelLoader = levelLoader;
        _itemPool = itemPool;
        _cameraController = cameraController;
    }

    public void InitializeLevel(int levelIndex)
    {
        _levelLoader ??= new LevelLoader();
        _itemPool ??= ItemPool.Instance;

        if (_itemPool == null)
        {
            Debug.LogError("❌ [BoardManager] Critical Error: No item pool in the scene.");
            return;
        }

        LevelData data = _levelLoader.LoadLevel(levelIndex);
        if (data == null) return; // Avoid errors if the level does not exist

        ApplyLevelData(data);

        // Notify the pool the amount of items to prepare
        int totalItemsInLevel = pipes.Sum(p => p.itemsQueue.Count);
        _itemPool.PreparePool(totalItemsInLevel);

        // Framed first: the board panel fills the area the camera leaves free
        _cameraController?.AdjustToBoard(width, height, traySpace);

        GenerateBoard();

        // Lets obstacles that aren't pipe items (the fog) build from the level data
        GameEvents.TriggerLevelLoaded(data);

        trayManager?.InitializeTray();

        // Lets obstacles evaluate the starting board
        GameEvents.TriggerBoardChanged();
    }

    void GenerateBoard()
    {
        // The tilemap is only the coordinate grid; pipes, exits and the panel are drawn by the renderer
        CenterBoard();

        if (pipeBoardRenderer != null)
        {
            Rect? frame = _cameraController != null && _cameraController.TryGetBoardFrame(out Rect f) ? f : (Rect?)null;
            pipeBoardRenderer.Build(pipes, width, height, GridToWorld, frame);
        }
        else
        {
            Debug.LogWarning("[BoardManager] No PipeBoardRenderer assigned: the pipes won't be drawn.");
        }

        InitializePipesAndItems();
    }

    // Loads the level data from a JSON file and populates the BoardManager's properties
    private void ApplyLevelData(LevelData data)
    {
        // Apply the parsed data to the BoardManager
        currentLevelNumber = data.levelNumber;
        width = data.width;
        height = data.height;

        // Translate PipeData back into gameplay Pipes
        pipes = new List<Pipe>();
        foreach (PipeData pd in data.pipes)
        {
            Pipe newPipe = new Pipe
            {
                layer = pd.layer,
                itemsQueue = pd.BuildSpawnQueue(),
                path = new List<Vector2Int>()
            };

            foreach (Vector2IntData v in pd.path)
            {
                newPipe.path.Add(new Vector2Int(v.x, v.y));
            }

            pipes.Add(newPipe);
        }
    }

    /// <summary>Logical color of a colorId, from the palette.</summary>
    public Color GetItemColor(int colorId)
    {
        return itemPalette != null ? itemPalette.GetColor(colorId) : Color.white;
    }

    // Helper function to find the highest layer pipe at a specific coordinate
    public Pipe GetHighestLayerPipeAt(Vector2Int pos)
    {
        return pipes.Where(p => p.path != null && p.path.Contains(pos))
                    .OrderByDescending(p => p.layer)
                    .FirstOrDefault();
    }

    /// <summary>World position of the center of a grid cell.</summary>
    public Vector3 GridToWorld(Vector2Int pos)
    {
        Vector3 cellWorldPos = tilemap.CellToWorld(new Vector3Int(pos.x, pos.y, 0));
        Vector3 centerOffset = new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);
        return cellWorldPos + centerOffset;
    }

    // Moves the tilemap to center it in the scene based on its width and height
    void CenterBoard()
    {
        float offsetX = -width / 2f;
        float offsetY = -height / 2f;
        tilemap.transform.position = new Vector3(offsetX, offsetY, 0);
    }

    // Initializes the items on each pipe based on their queue and path
    void InitializePipesAndItems()
    {
        foreach (Pipe pipe in pipes)
        {
            if (!pipe.HasPath) continue;

            pipe.activeItems.Clear();
            FillPipe(pipe);
        }
    }

    // Spawns an item GameObject at the specified grid position, initializes it, and returns the Item component
    Item SpawnItemObject(Vector2Int pos, ItemSpawnData data, Pipe pipe)
    {
        // Extract object from pool
        Item itemComponent = _itemPool.GetItem();
        GameObject itemObj = itemComponent.gameObject;

        itemObj.name = $"Item_P{pipe.layer}_{pos.x}_{pos.y}";
        itemObj.transform.SetParent(this.transform);
        itemObj.transform.position = GridToWorld(pos);

        itemComponent.SetPalette(itemPalette);
        itemComponent.SetMotionSettings(itemMotion);
        itemComponent.Init(data, GetItemColor(data.colorId), pos, pipe);

        //Hides hidden items
        itemComponent.ApplyHiddenLook();

        if (data.type == ItemType.Barrel)
        {
            int lives = data.lives > 0 ? data.lives : defaultBarrelLives;
            itemComponent.ApplyBarrelLook(lives, barrelDamageSprites);
        }

        // Under a higher pipe: hidden while it rests there (it stays active so it can slide out)
        itemObj.SetActive(true);
        itemComponent.SetCovered(GetHighestLayerPipeAt(pos) != pipe);

        return itemComponent;
    }

    private ItemMotionSettings MotionSettings => itemMotion != null ? itemMotion : ItemMotionSettings.Defaults;

    /// <summary>
    /// Moves an already spawned item to a new cell of its pipe.
    /// The grid position changes at once; the drawing slides there after 'delay' seconds (the chain).
    /// Cells under a higher pipe too: the item slides under it (drawn below it) and is hidden only once it
    /// rests there, so with fast taps it never vanishes before reaching a visible cell.
    /// </summary>
    private void PlaceItem(Item item, Pipe pipe, Vector2Int pos, float delay = 0f)
    {
        float shownScale = item.CurrentVisualScale;   // before the grid change sets the platform size
        item.UpdateGridPosition(pos);

        item.MoveAlongPipe(GridToWorld(pos), delay, null, shownScale);
        item.SetCovered(GetHighestLayerPipeAt(pos) != pipe);
    }

    /// <summary>Where a new item comes in from: behind the first cell of the pipe, at the inlet mouth.</summary>
    public Vector3 InletWorldPosition(Pipe pipe)
    {
        Vector3 first = GridToWorld(pipe.path[0]);
        Vector3 before = GridToWorld(PipePieceResolver.VirtualInletCell(pipe.path));
        return Vector3.LerpUnclamped(first, before, MotionSettings.inletOffset);
    }

    /// <summary>
    /// Spawns queued elements into the free cells of the pipe until the path is full
    /// or the queue runs out. activeItems[i] always sits at path[pathLen - 1 - i].
    /// </summary>
    /// <param name="animate">New items slide in through the inlet (false when the level is built: they are just there).</param>
    /// <param name="firstDelay">Delay of the first new item; each next one waits one more stagger step.</param>
    private void FillPipe(Pipe pipe, bool animate = false, float firstDelay = 0f)
    {
        if (!pipe.HasPath) return;

        int pathLen = pipe.path.Count;
        float delay = firstDelay;
        while (pipe.activeItems.Count < pathLen && pipe.activeItems.Count < pipe.itemsQueue.Count)
        {
            int index = pipe.activeItems.Count;
            Vector2Int pos = pipe.path[pathLen - 1 - index];

            Item newItem = SpawnItemObject(pos, pipe.itemsQueue[index], pipe);
            pipe.activeItems.Add(newItem);

            if (animate && pipe.path.Count >= 2)
            {
                newItem.MoveAlongPipe(GridToWorld(pos), delay, InletWorldPosition(pipe));
                delay += MotionSettings.stepStagger;
            }
        }
    }

    /// <summary>
    /// Removes the element at 'index' from the pipe (both the GameObject list and the queue), shifts every element behind it one step forward and refills the pipe from its queue.
    /// Returns the removed item (still alive, the caller decides what to do with it).
    /// </summary>
    private Item RemoveItemAt(Pipe pipe, int index)
    {
        Item removedItem = pipe.activeItems[index];
        Vector2Int removedCell = removedItem.gridPosition; // Where it was before leaving the board
        pipe.activeItems.RemoveAt(index);
        if (index < pipe.itemsQueue.Count) pipe.itemsQueue.RemoveAt(index);

        int pathLen = pipe.path.Count;

        // Shift every item that was behind the removed one forward by one step.
        // The one closest to the exit moves first and each next one a bit later: a wave up the pipe.
        ItemMotionSettings motion = MotionSettings;
        for (int i = index; i < pipe.activeItems.Count; i++)
        {
            PlaceItem(pipe.activeItems[i], pipe, pipe.path[pathLen - 1 - i], motion.StepDelay(i - index));
        }

        // The next one from the queue comes in through the inlet at the end of the wave
        FillPipe(pipe, true, motion.StepDelay(pipe.activeItems.Count - index));

        // Every item that leaves the board (tap or Match booster) can damage neighbor barrels
        GameEvents.TriggerItemRemovedFromBoard(removedItem, pipe, removedCell);

        return removedItem;
    }

    // Call this method when a candy is picked at the end of the pipe
    public void AdvancePipe(Pipe pipe)
    {
        if (pipe.activeItems.Count == 0) return;

        RemoveItemAt(pipe, 0);
        GameEvents.TriggerBoardChanged();
    }

    // ------- Barrels --------

    /// <summary>Removes lives from a barrel and breaks it when it runs out of them. Called by the BarrelManager when an item leaves the board next to it.</summary>
    public void DamageBarrel(Item barrel, int amount = 1)
    {
        if (barrel == null || !barrel.IsBarrel) return;

        bool destroyed = barrel.TakeBarrelDamage(amount);

        ItemSpawnData queueEntry = FindQueueEntry(barrel);
        if (queueEntry != null) queueEntry.lives = barrel.BarrelLives;

        if (destroyed) BreakBarrel(barrel);
    }

    /// <summary> Breaks a barrel, leaving in its place the element it was holding (normal or hidden).</summary>
    public void BreakBarrel(Item barrel)
    {
        if (barrel == null || !barrel.IsBarrel) return;

        barrel.BreakOpen();
        barrel.ApplyHiddenLook(); // Only if the content was hidden

        // Keep the queue in sync with what's on the board
        ItemSpawnData queueEntry = FindQueueEntry(barrel);
        if (queueEntry != null)
        {
            queueEntry.type = barrel.itemType;
            queueEntry.lives = 0;
        }

        GameEvents.TriggerBoardChanged();
    }

    // The queue entry that describes an item currently on the board (same index as in activeItems)
    private ItemSpawnData FindQueueEntry(Item item)
    {
        if (item.parentPipe == null) return null;

        int index = item.parentPipe.activeItems.IndexOf(item);
        return index >= 0 && index < item.parentPipe.itemsQueue.Count ? item.parentPipe.itemsQueue[index] : null;
    }

    // Checks if all items on all pipes have been collected
    public bool IsBoardCleared()
    {
        foreach (Pipe pipe in pipes)
        {
            if (!pipe.IsEmpty) return false;
        }
        return true;
    }

    /// <summary>
    /// Returns the distinct colors currently present among the active items on the board.
    /// Used by the Match booster to pick a random target color when the tray is empty.
    /// </summary>
    public List<int> GetActiveColors(bool includeHidden = false, bool includeBarrels = false)
    {
        return pipes.SelectMany(p => p.activeItems)
                    .Where(i => i.HasKnownColor || (includeHidden && i.IsHidden) || (includeBarrels && i.IsBarrel))
                    .Select(i => i.colorID)
                    .Distinct()
                    .ToList();
    }

    /// <summary>
    /// Finds and removes the first item of the given color, for the Match booster, which can reach into any pipe.
    /// First on the board, anywhere in any pipe (not just its exit): the gap it leaves collapses and a replacement
    /// comes in from that pipe's queue. If the board has none, it is taken from a queue (elements not spawned yet,
    /// only counted by the "+N" at the inlet): it is created at that inlet and the counter goes down.
    /// In each place, normal items first, then hidden ones, then barrels (broken open).
    /// Returns null if there is none left anywhere.
    /// </summary>
    public Item ExtractAnyItemOfColor(int colorId)
    {
        Item extracted = TryExtractFirst(i => i.HasKnownColor && i.colorID == colorId);
        if (extracted == null) extracted = TryExtractFirst(i => i.IsHidden && i.colorID == colorId);
        if (extracted == null) extracted = TryExtractFirst(i => i.IsBarrel && i.colorID == colorId);
        if (extracted == null) extracted = TryExtractFromQueue(d => d.type == ItemType.Normal && d.colorId == colorId);
        if (extracted == null) extracted = TryExtractFromQueue(d => d.type == ItemType.Hidden && d.colorId == colorId);
        if (extracted == null) extracted = TryExtractFromQueue(d => d.type == ItemType.Barrel && d.colorId == colorId);
        return extracted;
    }

    // Takes the first waiting (not spawned) queue entry that matches the condition: removes it from the queue and
    // creates its item at the pipe's inlet, so it flies to the tray from there. Nothing on the board moves.
    private Item TryExtractFromQueue(System.Predicate<ItemSpawnData> condition)
    {
        foreach (Pipe pipe in pipes)
        {
            if (!pipe.HasPath) continue;

            // Entries from activeItems.Count on are still waiting outside the board
            for (int i = pipe.activeItems.Count; i < pipe.itemsQueue.Count; i++)
            {
                ItemSpawnData data = pipe.itemsQueue[i];
                if (!condition(data)) continue;

                pipe.itemsQueue.RemoveAt(i);

                Item item = SpawnItemObject(pipe.path[0], data, pipe);
                item.SetCovered(false);   // even if a higher pipe covers the first cell: it flies to the tray
                // From the inlet mouth; a one-cell pipe has no tube nor inlet, so from its only cell
                item.transform.position = PipePieceResolver.HasTube(pipe.path) ? InletWorldPosition(pipe) : GridToWorld(pipe.path[0]);

                if (item.IsBarrel) item.BreakOpen();

                GameEvents.TriggerBoardChanged();   // the inlet counter goes down
                return item;
            }
        }

        return null;
    }

    // Removes the first active item (searching pipe by pipe) that matches the condition
    private Item TryExtractFirst(System.Predicate<Item> condition)
    {
        foreach (Pipe pipe in pipes)
        {
            int index = pipe.activeItems.FindIndex(condition);
            if (index == -1) continue;

            Item target = pipe.activeItems[index];
            target.SetCovered(false);   // it leaves for the tray: drawn all the way, even from under a higher pipe

            if (target.IsBarrel) target.BreakOpen();

            Item extractedItem = RemoveItemAt(pipe, index);

            GameEvents.TriggerBoardChanged();
            return extractedItem;
        }

        return null;
    }

    /// <summary>
    /// Randomly reshuffles the colors of every item currently spawned across all pipes,
    /// keeping their positions/paths untouched. Used by the Shuffle booster.
    /// </summary>
    public void ShuffleActiveItems()
    {
        // Every (pipe, index) slot that currently holds a shuffleable item
        List<(Pipe pipe, int index, Item item)> slots = new List<(Pipe, int, Item)>();
        foreach (Pipe pipe in pipes)
        {
            for (int i = 0; i < pipe.activeItems.Count; i++)
            {
                Item item = pipe.activeItems[i];
                if (item.HasKnownColor || item.IsHidden)
                    slots.Add((pipe, i, item));
            }
        }

        if (slots.Count < 2) return;

        // Fisher-Yates shuffle of the (color, type) pairs, then redistribute them across the same slots
        List<(int colorId, ItemType type)> identities = slots.Select(s => (colorId: s.item.colorID, type: s.item.itemType)).ToList();
        for (int i = identities.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (identities[i], identities[j]) = (identities[j], identities[i]);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            var (pipe, index, item) = slots[i];
            var (newColorId, newType) = identities[i];

            item.SetIdentity(newColorId, GetItemColor(newColorId), newType);
            item.ApplyHiddenLook(); // Does nothing if it ended up as a normal item

            // Keep the queue in sync with what's on the board
            pipe.itemsQueue[index].colorId = newColorId;
            pipe.itemsQueue[index].type = newType;
        }

        GameEvents.TriggerBoardChanged();
    }

    /// <summary>
    /// Adds an item of the given color into a random pipe's queue, spawning it immediately
    /// if that pipe has room to show it. Used by the TrayClearer booster to redistribute
    /// items taken out of the tray back onto the board.
    /// </summary>
    public void AddItemToRandomPipe(int colorId)
    {
        List<Pipe> eligiblePipes = pipes.Where(p => p.HasPath).ToList();
        if (eligiblePipes.Count == 0) return;

        Pipe targetPipe = eligiblePipes[Random.Range(0, eligiblePipes.Count)];
        targetPipe.itemsQueue.Add(ItemSpawnData.Normal(colorId));

        // If the pipe still has a free spot on its path, the new item comes in right away
        FillPipe(targetPipe, true);

        GameEvents.TriggerBoardChanged();
    }
}
