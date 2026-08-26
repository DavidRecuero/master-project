using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Pipe
{
    [Tooltip("Higher layer value will be drawn on top of lower layer pipes")]
    public int layer;

    [Tooltip("List of positions. Index 0 is the origin, the last index is the destination")]
    public List<Vector2Int> path;

    [Tooltip("Queue of items IDs (0, 1, 2...). Must match possibleItemColors indices.")]
    public List<int> itemsQueue = new List<int>();

    // Runtime list tracking the active GameObjects sitting on the path
    [HideInInspector]
    public List<Item> activeItems = new List<Item>();
}

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public TileBase[] tiles; // Different possible tiles to use in the board
    public TrayManager trayManager;

    [Header("Level Data")]
    private int currentLevelNumber;
    public int CurrentLevelNumber => currentLevelNumber;

    [Header("Board Data")]
    private int width;
    private int height;
    private List<Pipe> pipes = new List<Pipe>();
    public float traySpace = 2.5f;  // Extra space reserved at the bottom for the tray

    [Header("Pipes Lines Data")]
    public float alph = 0.4f; // Transparency for the pipe lines
    public float lineWidth = 0.05f; // Width of the pipe lines
    public int lineSortingOffset = 4; // Sorting order offset for the pipe lines

    [Header("Item Settings")]
    public Sprite itemSprite;
    public Color[] possibleItemColors = new Color[] { Color.red, Color.blue, Color.yellow, Color.green };

    [Header("Camera Settings")]
    public float edgeMargin = 1f; // Extra space around the board
    public float cameraZDepth = -10f;

    public void InitializeLevel(int levelIndex)
    {
        LevelData data = LevelLoader.LoadLevel(levelIndex);

        if (data == null) return; // Avoid errors if the level does not exist

        ApplyLevelData(data);

        GenerateBoard();
        AdjustCamera();
        trayManager.InitializeTray();
    }

    void GenerateBoard()
    {
        //Clear the tilemap before generating the board
        tilemap.ClearAllTiles();

        // --- PRECALCULATE PIPE COLORS ---

        // Sort all pipes from lowest layer to highest layer to precalculate their colors
        List< Pipe > sortedPipes = pipes.OrderBy(p => p.layer).ToList();
        Dictionary<Pipe, Color> pipeColors = new Dictionary<Pipe, Color>();

        int count = sortedPipes.Count;
        for (int i = 0; i < count; i++)
        {
            if (count == 1)
            {
                // If there's only 1 pipe, it's black
                pipeColors[sortedPipes[i]] = Color.black;
            }
            else
            {
                // If there are multiple pipes, interpolate between White (lowest) and Black (highest)
                // i = 0 (lowest) -> grayscale = 1f (White)
                // i = count - 1 (highest) -> grayscale = 0f (Black)
                float grayscale = 1f - ((float)i / (count - 1));
                pipeColors[sortedPipes[i]] = new Color(grayscale, grayscale, grayscale, 1f);
            }
        }


        // --- DRAW THE BOARD ---
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                Vector2Int currentPos2D = new Vector2Int(x, y);

                // Draw the base tile
                tilemap.SetTile(position, tiles[0]);

                // Unlock the tile so we can change its color
                tilemap.SetTileFlags(position, TileFlags.None);

                // Check if there is a pipe at this exact coordinate
                Pipe topPipe = GetHighestLayerPipeAt(currentPos2D);

                if (topPipe != null)
                {
                    // Paint it with the precalculated grayscale color
                    tilemap.SetColor(position, pipeColors[topPipe]);
                }
                else
                {
                    // Background: Fully transparent
                    tilemap.SetColor(position, Color.clear);
                }
            }
        }

        CenterBoard();

        DrawPipeLines(pipeColors);

        // --- SPAWN ITEMS ---
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
                itemsQueue = new List<int>(pd.itemsQueue),
                path = new List<Vector2Int>()
            };

            foreach (Vector2IntData v in pd.path)
            {
                newPipe.path.Add(new Vector2Int(v.x, v.y));
            }

            pipes.Add(newPipe);
        }
    }

    // Helper function to find the highest layer pipe at a specific coordinate
    private Pipe GetHighestLayerPipeAt(Vector2Int pos)
    {
        return pipes.Where(p => p.path != null && p.path.Contains(pos))
                    .OrderByDescending(p => p.layer)
                    .FirstOrDefault();
    }

    // Moves the tilemap to center it in the scene based on its width and height
    void CenterBoard()
    {
        float offsetX = -width / 2f;
        float offsetY = -height / 2f;
        tilemap.transform.position = new Vector3(offsetX, offsetY, 0);
    }

    // Adjusts the camera to ensure the entire board is visible with a margin
    void AdjustCamera()
    {
        Camera cam = Camera.main;

        // Extra space reserved at the bottom for the tray
        float totalHeight = height + traySpace;

        cam.transform.position = new Vector3(0, -traySpace / 2f, cameraZDepth);

        float sizeForHeight = totalHeight / 2f;
        float sizeForWidth = (width / 2f) / cam.aspect;

        //Pick the larger size(so nothing gets cut off) and add the margin
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) + edgeMargin;
    }

    // Initializes the items on each pipe based on their queue and path
    void InitializePipesAndItems()
    {
        foreach (Pipe pipe in pipes)
        {
            if (pipe.path == null || pipe.path.Count == 0) continue;

            pipe.activeItems.Clear();

            int pathLen = pipe.path.Count;
            int queueLen = pipe.itemsQueue.Count;
            int itemsToSpawn = Mathf.Min(pathLen, queueLen);

            for (int i = 0; i < itemsToSpawn; i++)
            {
                int pathIndex = pathLen - 1 - i;
                Vector2Int pos = pipe.path[pathIndex];

                int idColor = pipe.itemsQueue[i];
                Color color = possibleItemColors[idColor];

                Item item = SpawnItemObject(pos, idColor, color, pipe);
                pipe.activeItems.Add(item);
            }
        }
    }

    // Spawns an item GameObject at the specified grid position, initializes it, and returns the Item component
    Item SpawnItemObject(Vector2Int pos, int idColor, Color color, Pipe pipe)
    {
        GameObject itemObj = new GameObject($"Item_P{pipe.layer}_{pos.x}_{pos.y}");
        itemObj.transform.SetParent(this.transform);

        Vector3 cellWorldPos = tilemap.CellToWorld(new Vector3Int(pos.x, pos.y, 0));
        Vector3 centerOffset = new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);
        itemObj.transform.position = cellWorldPos + centerOffset;

        Item itemComponent = itemObj.AddComponent<Item>();
        itemComponent.Init(idColor, color, pos, pipe, itemSprite);

        // Visibility Check
        Pipe topPipe = GetHighestLayerPipeAt(pos);
        itemObj.SetActive(topPipe == pipe);

        return itemComponent;
    }

    // Call this method when a candy is picked at the end of the pipe
    public void AdvancePipe(Pipe pipe)
    {
        if (pipe.activeItems.Count == 0) return;

        // Remove the first item from the active list and the queue
        pipe.activeItems.RemoveAt(0);
        if (pipe.itemsQueue.Count > 0) pipe.itemsQueue.RemoveAt(0);

        int pathLen = pipe.path.Count;

        // Move all remaining active items one step forward along the path
        for (int i = 0; i < pipe.activeItems.Count; i++)
        {
            int pathIndex = pathLen - 1 - i;
            Vector2Int newPos = pipe.path[pathIndex];

            Item item = pipe.activeItems[i];
            item.gridPosition = newPos;

            Vector3 cellWorldPos = tilemap.CellToWorld(new Vector3Int(newPos.x, newPos.y, 0));
            Vector3 centerOffset = new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);
            item.transform.position = cellWorldPos + centerOffset;

            // Update visibility based on the highest layer pipe at the new position
            Pipe topPipe = GetHighestLayerPipeAt(newPos);
            item.gameObject.SetActive(topPipe == pipe);
        }

        // If there are still items in the queue, spawn a new item at the origin of the pipe
        if (pipe.itemsQueue.Count >= pathLen)
        {
            // The item that enters is the one at the index equivalent to the path size minus 1
            int newCandyIndex = pathLen - 1;
            int idColor = pipe.itemsQueue[newCandyIndex];
            Color color = possibleItemColors[idColor];

            // The origin is always the index 0 of the path
            Vector2Int originPos = pipe.path[0];

            Item newItem = SpawnItemObject(originPos, idColor, color, pipe);
            pipe.activeItems.Add(newItem);
        }
    }

    // Checks if all items on all pipes have been collected
    public bool IsBoardCleared()
    {
        foreach (Pipe pipe in pipes)
        {
            if (pipe.activeItems.Count > 0 || pipe.itemsQueue.Count > 0)
                return false;
        }
        return true;
    }

    private void DrawPipeLines(Dictionary<Pipe, Color> pipeColors)
    {
        Vector3 centerOffset = new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);

        foreach (Pipe pipe in pipes)
        {
            if (pipe.path == null || pipe.path.Count < 2) continue;

            GameObject lineObj = new GameObject($"PipeLine_Layer_{pipe.layer}");
            lineObj.transform.SetParent(tilemap.transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));

            // Forces the line to always face the camera
            lr.alignment = LineAlignment.TransformZ;

            // Soft colour based on the pipe's layer
            Color lineColor = pipeColors[pipe];
            lineColor.a = alph;
            lr.startColor = lineColor;
            lr.endColor = lineColor;

            // Line width and sorting order
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.sortingOrder = lineSortingOffset + pipe.layer;

            lr.positionCount = pipe.path.Count;
            for (int i = 0; i < pipe.path.Count; i++)
            {
                Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pipe.path[i].x, pipe.path[i].y, 0)) + centerOffset;
                lr.SetPosition(i, worldPos);
            }
        }
    }
}