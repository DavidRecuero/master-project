using UnityEngine;

public class Item : MonoBehaviour
{
    public int colorID;
    public Color itemColor;
    public Vector2Int gridPosition;
    public Pipe parentPipe;

    [Header("Item Settings")]
    public float defaultScale = 0.7f;
    public int baseSortingOrder = 10;
    public Vector2 colliderSize = new Vector2(1f, 1f);

    [Header("Animation Settings")]
    public float pulseSpeed = 14f;
    public float pulseAmount = 0.02f;
    [HideInInspector] public bool inTray = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void Init(int id, Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        colorID = id;
        itemColor = color;
        gridPosition = pos;
        parentPipe = pipe;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        // Slightly scale down the item so it fits nicely inside the pipe
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);

        // Draw above the tilemap (Tilemap is usually layer 0)
        spriteRenderer.sortingOrder = baseSortingOrder + pipe.layer;

        // Collider to detect clicks or interactions
        BoxCollider2D col = gameObject.GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.enabled = true;
    }

    private void Update()
    {
        // Check if this item is at the exit position of its pipe
        Vector2Int exitPosition = parentPipe.path[parentPipe.path.Count - 1];

        if (gridPosition == exitPosition && !inTray)
        {
            // Calculate a smooth pulsing value using Sine wave
            float currentPulse = defaultScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(currentPulse, currentPulse, 1f);
        }
        else
        {
            // Ensure the scale is reset if it's not at the exit
            transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
        }
    }
}