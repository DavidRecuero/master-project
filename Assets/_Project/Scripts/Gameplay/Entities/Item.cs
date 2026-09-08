using System.Collections;
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
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        EnsureSpriteRenderer();
    }

    private void OnEnable()
    {
        // Evaluates the animation on enable the game object
        CheckExitPulse();
    }

    private void OnDisable()
    {
        // Stops the courrotine if the GameObject is disabled
        StopPulse();
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null && !TryGetComponent(out spriteRenderer))
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

        EnsureSpriteRenderer();

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

        CheckExitPulse();
    }

    public void UpdateGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
        CheckExitPulse();
    }

    /// <summary>
    /// Recolors this item in place, without touching its position or parent pipe.
    /// Used by the Shuffle booster to redistribute colors across the board.
    /// </summary>
    public void ApplyColor(int id, Color color)
    {
        colorID = id;
        itemColor = color;

        EnsureSpriteRenderer();
        spriteRenderer.color = color;
    }

    public void CheckExitPulse()
    {
        if (inTray || parentPipe == null || parentPipe.path == null || parentPipe.path.Count == 0)
        {
            StopPulse();
            return;
        }

        Vector2Int exitPosition = parentPipe.path[parentPipe.path.Count - 1];
        if (gridPosition == exitPosition)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
    }

    public void StartPulse()
    {
        if (pulseCoroutine != null || !gameObject.activeInHierarchy) return;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
    }

    private IEnumerator PulseRoutine()
    {
        while (!inTray)
        {
            float currentPulse = defaultScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(currentPulse, currentPulse, 1f);
            yield return null;
        }
        StopPulse();
    }
}