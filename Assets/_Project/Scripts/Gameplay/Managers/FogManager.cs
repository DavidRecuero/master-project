using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Fog obstacle: a connected group of cells drawn as clouds on top of the board, hiding the
/// color of whatever sits underneath. Every time the PLAYER taps an item that goes to the tray,
/// each fog cell moves to a neighbor cell (see FogMover), so the cloud drifts and changes shape.
/// It never covers a pipe exit, so items that are ready to be tapped are always visible.
/// </summary>
public class FogManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;

    [Header("Visuals")]
    [Tooltip("Cloud sprite drawn on each fog cell. If empty, a plain square is used.")]
    public Sprite fogSprite;
    public Color fogColor = new Color(0.86f, 0.89f, 0.95f, 1f);
    [Tooltip("Must be above items (≈10-20) and below the tray (100)")]
    public int sortingOrder = 50;
    [Tooltip("Size of each cloud relative to a cell. Above 1 so neighbor clouds overlap and look like one.")]
    public float cellScale = 1.15f;
    public float moveDuration = 0.25f;

    [Header("Movement Rules")]
    [Tooltip("If on, the fog can only move over cells that belong to a pipe")]
    public bool onlyOverPipes = true;
    [Range(0f, 1f)]
    [Tooltip("Chance that each cell follows the shared direction of the move. Higher = drifts more as a whole.")]
    public float windBias = 0.6f;
    public int maxAttempts = 200;
    [Tooltip("0 = different every time. Any other value makes the movement repeatable (useful to debug).")]
    public int randomSeed = 0;

    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private readonly List<SpriteRenderer> clouds = new List<SpriteRenderer>();
    private System.Random rng;
    private Coroutine moveRoutine;

    /// <summary>Cells currently covered by fog.</summary>
    public IReadOnlyList<Vector2Int> Cells => cells;

    private void Awake()
    {
        boardManager ??= FindFirstObjectByType<BoardManager>();
    }

    public void Initialize(BoardManager board)
    {
        boardManager = board;
    }

    private void OnEnable()
    {
        GameEvents.OnLevelLoaded += Setup;
        GameEvents.OnItemCollectedByPlayer += HandleItemCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelLoaded -= Setup;
        GameEvents.OnItemCollectedByPlayer -= HandleItemCollected;
    }

    // ---------- Setup ----------

    /// <summary>Creates the fog described in the level data (does nothing if the level has no fog).</summary>
    public void Setup(LevelData data)
    {
        ClearFog();
        rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();

        if (data?.fog == null || data.fog.Count == 0) return;

        foreach (Vector2IntData c in data.fog)
        {
            cells.Add(new Vector2Int(c.x, c.y));
        }

        ValidateLevelFog(data.levelNumber);

        foreach (Vector2Int cell in cells)
        {
            clouds.Add(CreateCloud(cell));
        }
    }

    // Level design mistakes are reported, not fixed, so the designer notices them
    private void ValidateLevelFog(int levelNumber)
    {
        if (cells.Distinct().Count() != cells.Count)
            Debug.LogWarning($"[FogManager] Level {levelNumber}: the fog has repeated cells.");

        if (!FogMover.IsConnected(cells.ToList()))
            Debug.LogWarning($"[FogManager] Level {levelNumber}: the fog cells are not connected.");

        foreach (Vector2Int cell in cells.Where(c => !IsAllowedCell(c)))
            Debug.LogWarning($"[FogManager] Level {levelNumber}: fog cell {cell} is not allowed (outside the board, a pipe exit or not over a pipe).");
    }

    private SpriteRenderer CreateCloud(Vector2Int cell)
    {
        GameObject cloudObj = new GameObject($"Fog_{cell.x}_{cell.y}");
        cloudObj.transform.SetParent(transform, false);
        cloudObj.transform.position = boardManager.GridToWorld(cell);
        cloudObj.transform.localScale = new Vector3(cellScale, cellScale, 1f);

        SpriteRenderer sr = cloudObj.AddComponent<SpriteRenderer>();
        sr.sprite = fogSprite != null ? fogSprite : CreateFallbackSprite();
        sr.color = fogColor;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    // A 1x1 unit white square, used only if no cloud sprite was assigned
    private static Sprite CreateFallbackSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
    }

    private void ClearFog()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;

        foreach (SpriteRenderer cloud in clouds)
        {
            if (cloud != null) Destroy(cloud.gameObject);
        }
        clouds.Clear();
        cells.Clear();
    }

    // ---------- Movement ----------

    private void HandleItemCollected(Item item, Pipe sourcePipe) => MoveFog();

    /// <summary>Moves every fog cell one step, following the FogMover rules.</summary>
    public void MoveFog()
    {
        if (cells.Count == 0 || boardManager == null) return;

        rng ??= new System.Random();
        List<Vector2Int> next = FogMover.ComputeNextCells(cells, IsAllowedCell, rng, windBias, maxAttempts);
        if (next == null) return; // Boxed in, stays where it is

        cells.Clear();
        cells.AddRange(next);

        if (clouds.Count == cells.Count)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(AnimateClouds());
        }
    }

    /// <summary>
    /// Whether a fog cell can be on this cell: inside the board, never on a pipe exit
    /// (items there are ready to be tapped) and, optionally, only over pipes.
    /// </summary>
    public bool IsAllowedCell(Vector2Int cell)
    {
        if (boardManager == null) return false;
        if (cell.x < 0 || cell.y < 0 || cell.x >= boardManager.Width || cell.y >= boardManager.Height) return false;

        foreach (Pipe pipe in boardManager.Pipes)
        {
            if (pipe.HasPath && pipe.ExitPosition == cell) return false;
        }

        if (onlyOverPipes && boardManager.GetHighestLayerPipeAt(cell) == null) return false;

        return true;
    }

    // Slides every cloud from its current position to its new cell
    private IEnumerator AnimateClouds()
    {
        Vector3[] from = clouds.Select(c => c.transform.position).ToArray();
        Vector3[] to = cells.Select(c => boardManager.GridToWorld(c)).ToArray();

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);

            for (int i = 0; i < clouds.Count; i++)
                clouds[i].transform.position = Vector3.Lerp(from[i], to[i], t);

            yield return null;
        }

        for (int i = 0; i < clouds.Count; i++)
        {
            clouds[i].transform.position = to[i];
            clouds[i].name = $"Fog_{cells[i].x}_{cells[i].y}";
        }

        moveRoutine = null;
    }
}
