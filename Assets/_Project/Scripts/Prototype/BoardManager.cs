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
}

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public TileBase[] tiles; // Different possible tiles to use in the board

    [Header("Board Data")]
    public int width = 5;
    public int height = 5;

    [Header("Pipes Data")]
    public List<Pipe> pipes = new List<Pipe>();

    [Header("Camera Settings")]
    public float edgeMargin = 1f; // Extra space around the board

    void Start()
    {
        GenerateBoard();
        AdjustCamera();
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

        //Ensure it's looking at the center (0,0) but pulled back on Z (-10)
        cam.transform.position = new Vector3(0, 0, -10);

        float sizeForHeight = height / 2f;
        float sizeForWidth = (width / 2f) / cam.aspect;

        //Pick the larger size(so nothing gets cut off) and add the margin
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) + edgeMargin;
    }
}