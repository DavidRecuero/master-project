using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    public int colorID;
    public Color itemColor;
    public ItemType itemType = ItemType.Normal;
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

    /// <summary>True when this item sits on the last cell of its pipe.</summary>
    public bool IsAtExit =>
        parentPipe != null && parentPipe.HasPath && gridPosition == parentPipe.ExitPosition;

    /// <summary>
    /// Single source of truth for "can the player send this to the tray right now?".
    /// Obstacles (e.g. a barrel with lives left) will extend this rule.
    /// </summary>
    public bool CanBeCollected => !inTray && IsAtExit;

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

    /// <summary>Convenience overload for plain colored items.</summary>
    public void Init(int id, Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        Init(ItemSpawnData.Normal(id), color, pos, pipe, sprite);
    }

    public void Init(ItemSpawnData data, Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        colorID = data.colorId;
        itemType = data.type;
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

    /// <summary>
    /// Puts the item back into a neutral state before returning it to the pool,
    /// so no obstacle state leaks into the next element that reuses this GameObject.
    /// </summary>
    public void ResetForPool()
    {
        StopPulse();
        inTray = false;
        itemType = ItemType.Normal;
        parentPipe = null;
        if (TryGetComponent(out Collider2D col)) col.enabled = true;
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
        if (CanBeCollected)
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
