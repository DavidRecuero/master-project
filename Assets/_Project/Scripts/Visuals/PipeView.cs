using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws one pipe: tube pieces (with their shadow), inlet and outlet mouths, the exit platform and the
/// inlet counter with the number of elements still waiting in the queue.
/// Dumb view: it only reads the Pipe it was built for; PipeBoardRenderer tells it when to refresh.
/// </summary>
public class PipeView : MonoBehaviour
{
    private Pipe pipe;
    private PipeTileSet tileSet;

    private SpriteRenderer platformRenderer;
    private GameObject counterRoot;
    private TextMeshPro counterText;

    public Pipe Pipe => pipe;

    /// <summary>Elements waiting in the queue that are not on the board yet.</summary>
    public static int WaitingCount(Pipe pipe)
    {
        if (pipe == null || pipe.itemsQueue == null) return 0;
        int onBoard = pipe.activeItems != null ? pipe.activeItems.Count : 0;
        return Mathf.Max(0, pipe.itemsQueue.Count - onBoard);
    }

    /// <summary>The exit platform is lit while the pipe still has something on its path.</summary>
    public static bool IsPlatformActive(Pipe pipe)
    {
        return pipe != null && pipe.activeItems != null && pipe.activeItems.Count > 0;
    }

    /// <param name="depth">How many layers this pipe is below the top layer (0 = top).</param>
    public void Build(Pipe targetPipe, PipeTileSet set, Func<Vector2Int, Vector3> gridToWorld, int depth)
    {
        pipe = targetPipe;
        tileSet = set;
        if (pipe == null || !pipe.HasPath || tileSet == null) return;

        List<Vector2Int> path = pipe.path;
        int layer = pipe.layer;

        if (PipePieceResolver.HasTube(path))
        {
            // Tube + its shadow (the shadow only shows over lower layers and the panel)
            Color tint = tileSet.DepthTint(depth);
            Vector3 shadowOffset = new Vector3(tileSet.shadowOffset.x, tileSet.shadowOffset.y, 0f);

            foreach (PipePiece piece in PipePieceResolver.ResolvePieces(path))
            {
                Sprite sprite = tileSet.GetPiece(piece.Type);
                Vector3 pos = gridToWorld(piece.Cell);

                CreateRenderer($"Shadow_{piece.Cell.x}_{piece.Cell.y}", sprite, pos + shadowOffset,
                    BoardSortingOrder.For(layer, BoardSortingOrder.Shadow), tileSet.shadowColor, tileSet.pieceOverlapScale);
                CreateRenderer($"Tube_{piece.Cell.x}_{piece.Cell.y}", sprite, pos,
                    BoardSortingOrder.For(layer, BoardSortingOrder.Tube), tint, tileSet.pieceOverlapScale);
            }

            PipeMouth inlet = PipePieceResolver.InletMouth(path);
            PipeMouth outlet = PipePieceResolver.OutletMouth(path);
            CreateMouth(inlet, gridToWorld, layer, 0f);
            // The outlet mouth is pulled back into the last tube cell so it never overlaps the platform
            CreateMouth(outlet, gridToWorld, layer, tileSet.outletMouthInset);
            CreateCounter(inlet, gridToWorld, layer);
        }

        // Exit platform
        platformRenderer = CreateRenderer("Platform", tileSet.platformActive, gridToWorld(pipe.ExitPosition),
            BoardSortingOrder.For(layer, BoardSortingOrder.Platform), Color.white, 1f);

        Refresh();
    }

    /// <summary>Updates the platform state and the counter from the pipe's current data.</summary>
    public void Refresh()
    {
        if (pipe == null || tileSet == null) return;

        if (platformRenderer != null)
            platformRenderer.sprite = IsPlatformActive(pipe) ? tileSet.platformActive : tileSet.platformOff;

        if (counterRoot != null)
        {
            int waiting = WaitingCount(pipe);
            counterRoot.SetActive(waiting > 0 || !tileSet.hideCounterWhenEmpty);
            if (counterText != null) counterText.text = "+" + waiting;
        }
    }

    /// <param name="inset">How far the mouth is moved from the shared edge towards the tube, in cells.</param>
    private void CreateMouth(PipeMouth mouth, Func<Vector2Int, Vector3> gridToWorld, int layer, float inset)
    {
        Vector3 inside = gridToWorld(mouth.Inside);
        Vector3 outside = gridToWorld(mouth.Outside);
        Vector3 pos = (inside + outside) * 0.5f + (inside - outside) * inset;
        CreateRenderer("Mouth", tileSet.GetMouth(mouth.IsHorizontalBar), pos,
            BoardSortingOrder.For(layer, BoardSortingOrder.Mouth), Color.white, 1f);
    }

    private void CreateCounter(PipeMouth inlet, Func<Vector2Int, Vector3> gridToWorld, int layer)
    {
        if (tileSet.counterPlate == null) return;

        Vector3 inside = gridToWorld(inlet.Inside);
        Vector3 outside = gridToWorld(inlet.Outside);
        Vector3 mouthPos = (inside + outside) * 0.5f;
        Vector3 outward = outside - inside; // One cell long, pointing away from the tube

        counterRoot = new GameObject("InletCounter");
        counterRoot.transform.SetParent(transform, false);
        counterRoot.transform.position = mouthPos;

        if (tileSet.counterPost != null)
        {
            SpriteRenderer post = CreateRenderer("Post", tileSet.counterPost, mouthPos + outward * tileSet.counterPostDistance,
                BoardSortingOrder.For(layer, BoardSortingOrder.Counter), Color.white, 1f, counterRoot.transform);
            if (!inlet.IsHorizontalBar) post.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // Horizontal flow
        }

        Vector3 platePos = mouthPos + outward * tileSet.counterPlateDistance;
        CreateRenderer("Plate", tileSet.counterPlate, platePos,
            BoardSortingOrder.For(layer, BoardSortingOrder.Counter), Color.white, 1f, counterRoot.transform);

        GameObject textObj = new GameObject("Count");
        textObj.transform.SetParent(counterRoot.transform, false);

        counterText = textObj.AddComponent<TextMeshPro>(); // Swaps the Transform for a RectTransform
        counterText.rectTransform.position = platePos;
        if (tileSet.counterFont != null) counterText.font = tileSet.counterFont;
        counterText.fontSize = tileSet.counterFontSize;
        counterText.fontStyle = FontStyles.Bold;
        counterText.color = tileSet.counterTextColor;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.rectTransform.sizeDelta = new Vector2(0.8f, 0.4f);

        Renderer textRenderer = textObj.GetComponent<Renderer>();
        if (textRenderer != null) textRenderer.sortingOrder = BoardSortingOrder.For(layer, BoardSortingOrder.CounterText);
    }

    private SpriteRenderer CreateRenderer(string objName, Sprite sprite, Vector3 position, int order, Color color, float scale, Transform parent = null)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }
}
