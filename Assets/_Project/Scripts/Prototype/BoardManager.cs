using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public TileBase[] tiles; // Different possible tiles to use in the board

    [Header("Board Data")]
    public int width = 5;
    public int height = 5;

    [Header("Camera Settings")]
    public float edgeMargin = 1f; // Extra space around the board


    private int[,] boardMap = new int[,]
    {
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0 } 
    };

    void Start()
    {
        GenerateBoard();
        AdjustCamera();
    }

    void GenerateBoard()
    {
        //Clear the tilemap before generating the board
        tilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Get the tile ID from the boardMap array
                int idTile = boardMap[x, y];

                Vector3Int position = new Vector3Int(x, y, 0);

                // Draw the tile in the tilemap at the specified position
                tilemap.SetTile(position, tiles[idTile]);

                // --- DEBUG: RANDOM COLORS ---
                tilemap.SetTileFlags(position, TileFlags.None);
                Color randomColor = Random.ColorHSV();
                tilemap.SetColor(position, randomColor);
            }
        }

        CenterBoard();
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