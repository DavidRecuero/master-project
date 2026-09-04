using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PipeLineRenderer : MonoBehaviour
{
    [Header("Settings")]
    public float alpha = 0.4f;          // Transparency for the pipe lines
    public float lineWidth = 0.05f;     // Width of the pipe lines
    public int lineSortingOffset = 4;   // Sorting order offset for the pipe lines

    /// <summary>
    /// Draw LineRenderers of pipes hidden in layers under other pipes
    /// </summary>
    public void DrawPipeLines(List<Pipe> pipes, Dictionary<Pipe, Color> pipeColors, Tilemap tilemap)
    {
        // Clean previous lines if the level is regenerated
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        Vector3 centerOffset = new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);

        foreach (Pipe pipe in pipes)
        {
            if (pipe.path == null || pipe.path.Count < 2) continue;

            GameObject lineObj = new GameObject($"PipeLine_Layer_{pipe.layer}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));

            // Forces the line to always face the camera
            lr.alignment = LineAlignment.TransformZ;

            // Soft colour based on the pipe's layer
            Color lineColor = pipeColors.TryGetValue(pipe, out var color) ? color : Color.white;
            lineColor.a = alpha;
            lr.startColor = lineColor;
            lr.endColor = lineColor;

            // Line width and sorting order
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.sortingOrder = lineSortingOffset + pipe.layer;

            lr.positionCount = pipe.path.Count;
            for (int i = 0; i < pipe.path.Count; i++)
            {
                Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pipe.path[i].x, pipe.path[i].y, 0)) + centerOffset;
                lr.SetPosition(i, worldPos);
            }
        }
    }
}