using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the visual board: the panel behind the cells and one PipeView per pipe.
/// Refreshes platforms and inlet counters every time the board changes.
/// </summary>
public class PipeBoardRenderer : MonoBehaviour
{
    public PipeTileSet tileSet;

    private readonly List<PipeView> views = new List<PipeView>();
    private SpriteRenderer panelRenderer;
    private Transform root; // Everything this renderer creates lives here, so Clear never touches other children

    public IReadOnlyList<PipeView> Views => views;

    private void OnEnable()
    {
        GameEvents.OnBoardChanged += RefreshAll;
    }

    private void OnDisable()
    {
        GameEvents.OnBoardChanged -= RefreshAll;
    }

    /// <summary>
    /// Rebuilds every pipe view. Call after the board has been centred (GridToWorld must be final).
    /// frame = panel face rect from the camera (fills the free screen area); null fits the panel to the board.
    /// </summary>
    public void Build(IReadOnlyList<Pipe> pipes, int width, int height, Func<Vector2Int, Vector3> gridToWorld, Rect? frame = null)
    {
        Clear();
        if (tileSet == null || pipes == null) return;

        BuildPanel(width, height, gridToWorld, frame);

        // Darken only what is really covered by another pipe, not every layer below the top one
        Dictionary<Pipe, int> depths = PipeDepthCalculator.Compute(pipes);

        foreach (Pipe pipe in pipes)
        {
            if (pipe == null || !pipe.HasPath) continue;

            GameObject go = new GameObject($"PipeView_L{pipe.layer}");
            go.transform.SetParent(EnsureRoot(), false);

            PipeView view = go.AddComponent<PipeView>();
            view.Build(pipe, tileSet, gridToWorld, depths.TryGetValue(pipe, out int depth) ? depth : 0);
            views.Add(view);
        }
    }

    public void RefreshAll()
    {
        foreach (PipeView view in views)
        {
            if (view != null) view.Refresh();
        }
    }

    public void Clear()
    {
        views.Clear();
        panelRenderer = null;
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private Transform EnsureRoot()
    {
        if (root == null)
        {
            root = new GameObject("PipeVisuals").transform;
            root.SetParent(transform, false);
        }
        return root;
    }

    private void BuildPanel(int width, int height, Func<Vector2Int, Vector3> gridToWorld, Rect? frame)
    {
        if (tileSet.boardPanel == null || width <= 0 || height <= 0) return;

        Rect face = PanelFace(FittedFace(width, height, gridToWorld), frame);

        GameObject go = new GameObject("BoardPanel");
        go.transform.SetParent(EnsureRoot(), false);
        // The sprite includes the shadow below the face, so the whole sprite is shifted down by half of it
        go.transform.position = new Vector3(face.center.x, face.center.y - tileSet.panelShadowHeight * 0.5f, 0f);

        panelRenderer = go.AddComponent<SpriteRenderer>();
        panelRenderer.sprite = tileSet.boardPanel;
        panelRenderer.drawMode = SpriteDrawMode.Sliced;
        panelRenderer.size = new Vector2(face.width, face.height + tileSet.panelShadowHeight);
        panelRenderer.sortingOrder = BoardSortingOrder.Panel;
    }

    // Smallest panel face around the cells, with the tile set paddings
    private Rect FittedFace(int width, int height, Func<Vector2Int, Vector3> gridToWorld)
    {
        Vector3 bottomLeft = gridToWorld(new Vector2Int(0, 0));
        Vector3 topRight = gridToWorld(new Vector2Int(width - 1, height - 1));
        Vector3 cellsCenter = (bottomLeft + topRight) * 0.5f;

        return Rect.MinMaxRect(
            cellsCenter.x - width / 2f - tileSet.panelSidePadding,
            cellsCenter.y - height / 2f - tileSet.panelBottomPadding,
            cellsCenter.x + width / 2f + tileSet.panelSidePadding,
            cellsCenter.y + height / 2f + tileSet.panelTopPadding);
    }

    /// <summary>The camera's frame when there is one, grown if needed so it never cuts the board.</summary>
    public static Rect PanelFace(Rect fitted, Rect? frame)
    {
        if (!frame.HasValue) return fitted;

        Rect f = frame.Value;
        return Rect.MinMaxRect(
            Mathf.Min(f.xMin, fitted.xMin), Mathf.Min(f.yMin, fitted.yMin),
            Mathf.Max(f.xMax, fitted.xMax), Mathf.Max(f.yMax, fitted.yMax));
    }
}
