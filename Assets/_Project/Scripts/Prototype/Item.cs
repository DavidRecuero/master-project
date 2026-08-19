using UnityEngine;

public class Item : MonoBehaviour
{
    public int colorID;
    public Color itemColor;
    public Vector2Int gridPosition;
    public Pipe parentPipe;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void Init(Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        itemColor = color;
        gridPosition = pos;
        parentPipe = pipe;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        // Slightly scale down the item so it fits nicely inside the pipe
        transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // Draw above the tilemap (Tilemap is usually layer 0)
        spriteRenderer.sortingOrder = 10 + pipe.layer;
    }
}