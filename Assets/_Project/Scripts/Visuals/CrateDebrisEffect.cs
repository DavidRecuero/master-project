using System.Collections;
using UnityEngine;

/// <summary>
/// Visual feedback for the crate obstacle. Listens to GameEvents.OnBarrelDamaged:
/// on every hit the plank that disappears from the sprite flies off, and when the crate breaks
/// it bursts into splinters. Pure visuals: spawns a few short-lived sprites and destroys them.
/// </summary>
public class CrateDebrisEffect : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite plankSprite;
    public Sprite splinterSprite;

    [Header("Look")]
    [Tooltip("Lowest draw order of the flying pieces. Raised on each level so they fly above the highest pipe layer (and the fog).")]
    public int sortingOrder = 60;
    public float[] plankOffsets = { 0.25f, 0.0f, -0.23f };

    [Header("Motion")]
    public float duration = 0.55f;
    public float gravity = -7f;
    public float plankSpeed = 1.6f;
    public int splintersOnBreak = 7;
    public Vector2 splinterSpeed = new Vector2(1.4f, 2.6f);

    private int pieceOrder;

    private void Awake()
    {
        pieceOrder = sortingOrder;
    }

    private void OnEnable()
    {
        GameEvents.OnBarrelDamaged += HandleBarrelDamaged;
        GameEvents.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnBarrelDamaged -= HandleBarrelDamaged;
        GameEvents.OnLevelLoaded -= HandleLevelLoaded;
    }

    // Above the pipes and the fog (which takes the first two orders above the board)
    private void HandleLevelLoaded(LevelData data)
    {
        pieceOrder = Mathf.Max(sortingOrder, BoardSortingOrder.AboveBoard(data) + 5);
    }

    /// <summary>Which plank leaves the sprite when the crate goes down to `livesLeft` (3 planks art). -1 = none.</summary>
    public static int PlankRemovedAt(int livesLeft)
    {
        switch (livesLeft)
        {
            case 2: return 1; // 3 -> 2: the middle plank
            case 1: return 0; // 2 -> 1: the top plank (the bottom one stays, loose)
            default: return -1;
        }
    }

    private void HandleBarrelDamaged(Item barrel, int livesLeft)
    {
        if (barrel == null || !isActiveAndEnabled) return;

        Vector3 center = barrel.transform.position;
        float scale = barrel.transform.localScale.x;

        if (livesLeft <= 0)
        {
            // Burst: the last plank plus a handful of splinters
            SpawnPiece(plankSprite, center + new Vector3(0f, plankOffsets[2] * scale, 0f), scale,
                       new Vector2(RandomSign() * plankSpeed * 0.6f, plankSpeed * 1.2f));
            for (int i = 0; i < splintersOnBreak; i++)
            {
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(splinterSpeed.x, splinterSpeed.y);
                SpawnPiece(splinterSprite, center, scale, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed);
            }
            return;
        }

        int plank = PlankRemovedAt(livesLeft);
        if (plank >= 0 && plank < plankOffsets.Length)
        {
            SpawnPiece(plankSprite, center + new Vector3(0f, plankOffsets[plank] * scale, 0f), scale,
                       new Vector2(RandomSign() * plankSpeed, plankSpeed));
        }

        // A couple of splinters on every hit, also for crates with more lives than planks
        for (int i = 0; i < 2; i++)
        {
            SpawnPiece(splinterSprite, center, scale,
                       new Vector2(Random.Range(-1f, 1f), Random.Range(0.8f, 1.4f)) * splinterSpeed.x);
        }
    }

    private void SpawnPiece(Sprite sprite, Vector3 position, float scale, Vector2 velocity)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("CrateDebris");
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = pieceOrder;

        StartCoroutine(Fly(go.transform, sr, velocity, Random.Range(-540f, 540f)));
    }

    private IEnumerator Fly(Transform piece, SpriteRenderer sr, Vector2 velocity, float spin)
    {
        float t = 0f;
        while (t < duration && piece != null)
        {
            float dt = Time.deltaTime;
            t += dt;

            velocity.y += gravity * dt;
            piece.position += (Vector3)(velocity * dt);
            piece.Rotate(0f, 0f, spin * dt);

            // Fade out during the last 40%
            float fade = Mathf.InverseLerp(duration, duration * 0.6f, t);
            Color c = sr.color; c.a = fade; sr.color = c;

            yield return null;
        }
        if (piece != null) Destroy(piece.gameObject);
    }

    private static float RandomSign() => Random.value < 0.5f ? -1f : 1f;
}
