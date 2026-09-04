using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PipeLineRendererTests
{
    private GameObject _pipeLineObj;
    private PipeLineRenderer _pipelineRenderer;
    private Tilemap _tilemap;

    [SetUp]
    public void SetUp()
    {
        _pipeLineObj = new GameObject("PipeLineRenderer");
        _pipelineRenderer = _pipeLineObj.AddComponent<PipeLineRenderer>();

        var gridObj = new GameObject("Grid");
        var tilemapObj = new GameObject("Tilemap");
        tilemapObj.transform.SetParent(gridObj.transform);
        _tilemap = tilemapObj.AddComponent<Tilemap>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_pipeLineObj);
        if (_tilemap != null && _tilemap.transform.parent != null)
        {
            Object.DestroyImmediate(_tilemap.transform.parent.gameObject);
        }
    }

    [Test]
    public void DrawPipeLines_CreatesChildLineRenderer_PerValidPipe()
    {
        var pipes = new List<Pipe>
        {
            new Pipe { layer = 1, path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) } },
            new Pipe { layer = 2, path = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(1, 1) } }
        };

        var colors = new Dictionary<Pipe, Color>
        {
            { pipes[0], Color.red },
            { pipes[1], Color.blue }
        };

        _pipelineRenderer.DrawPipeLines(pipes, colors, _tilemap);

        Assert.AreEqual(2, _pipeLineObj.transform.childCount);
    }

    [Test]
    public void DrawPipeLines_IgnoresPipesWithInvalidPath()
    {
        var pipes = new List<Pipe>
        {
            new Pipe { layer = 1, path = new List<Vector2Int> { new Vector2Int(0, 0) } },
            new Pipe { layer = 2, path = null }
        };

        _pipelineRenderer.DrawPipeLines(pipes, new Dictionary<Pipe, Color>(), _tilemap);

        Assert.AreEqual(0, _pipeLineObj.transform.childCount);
    }

    [Test]
    public void DrawPipeLines_CleansPreviousLines_OnRegeneration()
    {
        var pipes = new List<Pipe>
        {
            new Pipe { layer = 1, path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) } }
        };

        _pipelineRenderer.DrawPipeLines(pipes, new Dictionary<Pipe, Color>(), _tilemap);
        _pipelineRenderer.DrawPipeLines(pipes, new Dictionary<Pipe, Color>(), _tilemap);

        Assert.AreEqual(1, _pipeLineObj.transform.childCount);
    }
}