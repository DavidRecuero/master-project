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
    // Item sprites are drawn at exit-platform size (200 px of a 256 px cell, PPU 256).
    // Inside the tube they are shown a bit smaller, and full size once they reach the exit.
    public float defaultScale = 0.86f;
    public float exitScaleMultiplier = 1.17f;
    [Tooltip("Order inside the pipe's layer band (see BoardSortingOrder): above its tube, below the mouths")]
    public int baseSortingOrder = BoardSortingOrder.Item;
    public Vector2 colliderSize = new Vector2(1f, 1f);

    [Header("Animation Settings")]
    public float pulseSpeed = 14f;
    public float pulseAmount = 0.02f;
    [HideInInspector] public bool inTray = false;

    private SpriteRenderer spriteRenderer;
    private Coroutine pulseCoroutine;

    // Finished sprite per color, plus the hidden sprite. Every look comes from here.
    private ItemPalette palette;

    // ---- Barrel state
    private int barrelLives;
    private Sprite[] barrelDamageSprites;   // [0] = intact, [last] = most damaged
    private bool barrelContentHidden;       // The element inside comes out as Hidden

    /// <summary>True when this item sits on the last cell of its pipe.</summary>
    public bool IsAtExit =>
        parentPipe != null && parentPipe.HasPath && gridPosition == parentPipe.ExitPosition;

    /// <summary>
    /// Single source of truth for "can the player send this to the tray right now?".
    /// Obstacles (e.g. a barrel with lives left) will extend this rule.
    /// </summary>
    public bool CanBeCollected => !inTray && IsAtExit && !IsBarrel;

    /// <summary>The player can't see this item's color yet.</summary>
    public bool IsHidden => itemType == ItemType.Hidden;

    /// <summary>Can't be tapped until it breaks and releases the element inside.</summary>
    public bool IsBarrel => itemType == ItemType.Barrel;

    /// <summary>Lives left (only for barrels).</summary>
    public int BarrelLives => barrelLives;

    /// <summary> A plain item whose color is visible. Boosters that work with colors (Match, Shuffle) only touch these, so they never reveal or move obstacles. </summary>
    public bool HasKnownColor => itemType == ItemType.Normal;

    /// <summary>Resting scale: full size on the exit platform, smaller inside the tube and in the tray.</summary>
    public float BaseScale => (!inTray && IsAtExit) ? defaultScale * exitScaleMultiplier : defaultScale;

    /// <summary>The palette this item uses to pick its sprite. Set it before Init.</summary>
    public ItemPalette Palette => palette;

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

    /// <summary>Assigns the palette used to draw this item: one finished sprite per color, plus the hidden sprite.</summary>
    public void SetPalette(ItemPalette itemPalette)
    {
        palette = itemPalette;
    }

    /// <summary>Convenience overload for plain colored items.</summary>
    public void Init(int id, Color color, Vector2Int pos, Pipe pipe)
    {
        Init(ItemSpawnData.Normal(id), color, pos, pipe);
    }

    public void Init(ItemSpawnData data, Color color, Vector2Int pos, Pipe pipe)
    {
        colorID = data.colorId;
        itemType = data.type;
        barrelContentHidden = data.contentHidden;
        barrelLives = 0;
        barrelDamageSprites = null;
        itemColor = color;
        gridPosition = pos;
        parentPipe = pipe;

        ShowColorLook();

        // Smaller inside the tube, full size on the exit platform
        SetScale(BaseScale);

        // Above its own tube, below every pipe of a higher layer
        spriteRenderer.sortingOrder = BoardSortingOrder.For(pipe.layer, baseSortingOrder);

        // Collider to detect clicks or interactions
        BoxCollider2D col = gameObject.GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.enabled = true;

        CheckExitPulse();
    }

    /// <summary>
    /// Covers the real color of a Hidden item with the palette's hidden sprite (grey drop with "?").
    /// The real color is kept in itemColor/colorID and shown again on Reveal().
    /// </summary>
    public void ApplyHiddenLook()
    {
        if (!IsHidden) return;

        EnsureSpriteRenderer();
        spriteRenderer.sprite = palette != null ? palette.HiddenSprite : null;
        spriteRenderer.color = Color.white;
    }

    /// <summary>
    /// Turns a Hidden item into a normal one showing its real color.
    /// Called by the tray right before the item is placed, so matching works as usual.
    /// </summary>
    public void Reveal()
    {
        if (!IsHidden) return;

        itemType = ItemType.Normal;

        ShowColorLook();
    }

    // ---------- Barrel ----------

    /// <summary>
    /// Sets up the barrel look and lives. damageSprites[0] is the intact barrel and each next sprite is more damaged.
    /// </summary>
    public void ApplyBarrelLook(int lives, Sprite[] damageSprites)
    {
        if (!IsBarrel) return;

        barrelLives = Mathf.Max(1, lives);
        barrelDamageSprites = damageSprites;

        UpdateBarrelSprite();
        CheckExitPulse();
    }

    /// <summary>Removes lives from the barrel. Returns true when it has no lives left.</summary>
    public bool TakeBarrelDamage(int amount = 1)
    {
        if (!IsBarrel) return false;

        int before = barrelLives;
        barrelLives = Mathf.Max(0, barrelLives - amount);
        UpdateBarrelSprite();

        // The last hit is announced by BreakOpen, when the crate actually bursts
        if (barrelLives > 0 && barrelLives < before) GameEvents.TriggerBarrelDamaged(this, barrelLives);

        return barrelLives == 0;
    }

    /// <summary>
    /// Turns the barrel into the element it was holding (normal or hidden), keeping its color id, position and pipe. Call ApplyHiddenLook afterwards to cover a hidden content.
    /// </summary>
    public void BreakOpen()
    {
        if (!IsBarrel) return;

        // Announced before the sprite changes, so effects still see the crate where it was
        GameEvents.TriggerBarrelDamaged(this, 0);

        itemType = barrelContentHidden ? ItemType.Hidden : ItemType.Normal;
        barrelLives = 0;
        barrelDamageSprites = null;

        ShowColorLook();

        CheckExitPulse();
    }

    // Shows the sprite that matches the damage taken so far
    private void UpdateBarrelSprite()
    {
        if (barrelDamageSprites == null || barrelDamageSprites.Length == 0) return;

        EnsureSpriteRenderer();
        int index = Mathf.Clamp(barrelDamageSprites.Length - barrelLives, 0, barrelDamageSprites.Length - 1);

        spriteRenderer.sprite = barrelDamageSprites[index];
        spriteRenderer.color = Color.white; // Show the barrel art untinted
    }

    // Shows the item's real color: the palette sprite for its colorID (the sprite already carries its color)
    private void ShowColorLook()
    {
        EnsureSpriteRenderer();
        spriteRenderer.sprite = palette != null ? palette.GetSprite(colorID) : null;
        spriteRenderer.color = Color.white;
    }

    private void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, 1f);
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
        barrelLives = 0;
        barrelDamageSprites = null;
        barrelContentHidden = false;
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
    /// A hidden item stores the new color but keeps showing its hidden look.
    /// </summary>
    public void ApplyColor(int id, Color color)
    {
        colorID = id;
        itemColor = color;

        if (IsHidden) return;

        ShowColorLook();
    }

    /// <summary>
    /// Changes color and type at once and shows the item as visible.
    /// Call ApplyHiddenLook afterwards if the new type is Hidden.
    /// </summary>
    public void SetIdentity(int id, Color color, ItemType type)
    {
        colorID = id;
        itemColor = color;
        itemType = type;

        ShowColorLook();
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
        SetScale(BaseScale);
    }

    private IEnumerator PulseRoutine()
    {
        while (!inTray)
        {
            SetScale(BaseScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            yield return null;
        }
        StopPulse();
    }
}
