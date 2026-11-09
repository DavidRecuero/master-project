using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// The Match booster also reaches the elements still waiting in the pipes' queues (only counted by the
/// "+N" at the inlet): when the board has no more of the color, the missing one comes out of a queue.
/// </summary>
public class MatchBoosterQueueTests
{
    private GameObject _boardObject;
    private GameObject _trayObject;
    private BoardManager _boardManager;
    private TrayManager _trayManager;
    private FakeItemPool _pool;
    private MatchBooster _booster;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _boardObject = new GameObject("Board");
        _boardObject.AddComponent<Grid>();
        _boardManager = _boardObject.AddComponent<BoardManager>();
        var tilemapGo = new GameObject("Tilemap");
        tilemapGo.transform.SetParent(_boardObject.transform);
        _boardManager.tilemap = tilemapGo.AddComponent<Tilemap>();

        _pool = new FakeItemPool();
        _boardManager.Initialize(null, _pool, new FakeCameraController());

        _trayObject = new GameObject("Tray");
        _trayManager = _trayObject.AddComponent<TrayManager>();
        _trayManager.Initialize(new FakeItemPool(), new FakeGameStateController(), new FakeCameraController());
        typeof(TrayManager).GetField("boardManager", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_trayManager, _boardManager);

        _booster = new MatchBooster(_trayManager, _boardManager);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned) if (go != null) Object.DestroyImmediate(go);
        _spawned.Clear();
        Object.DestroyImmediate(_trayObject);
        Object.DestroyImmediate(_boardObject);
    }

    // ---------- Helpers ----------

    /// <summary>
    /// A one-cell pipe (full with one element on the board) at column x. The first entry of the queue is the
    /// element on the board; the rest are waiting outside.
    /// </summary>
    private Pipe MakePipe(int x, params ItemSpawnData[] queue)
    {
        var pipe = new Pipe
        {
            path = new List<Vector2Int> { new Vector2Int(x, 0) },
            itemsQueue = queue.ToList(),
            activeItems = new List<Item>()
        };

        var go = new GameObject("OnBoard");
        _spawned.Add(go);
        Item onBoard = go.AddComponent<Item>();
        onBoard.Init(queue[0], Color.white, pipe.ExitPosition, pipe);
        pipe.activeItems.Add(onBoard);
        return pipe;
    }

    private void SetPipes(params Pipe[] pipes)
    {
        typeof(BoardManager).GetField("pipes", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_boardManager, pipes.ToList());
    }

    private List<Item> Tray => (List<Item>)typeof(TrayManager).GetField("trayItems", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_trayManager);

    private void PutInTray(int colorId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("InTray");
            _spawned.Add(go);
            Item item = go.AddComponent<Item>();
            item.colorID = colorId;
            Tray.Add(item);
        }
    }

    private static ItemSpawnData Hidden(int colorId) => new ItemSpawnData { colorId = colorId, type = ItemType.Hidden };
    private static ItemSpawnData Barrel(int colorId) => new ItemSpawnData { colorId = colorId, type = ItemType.Barrel, lives = 2 };

    // ---------- Board manager ----------

    [Test]
    public void Extract_ColorOnlyInAQueue_TakesItFromThere()
    {
        Pipe pipe = MakePipe(0, ItemSpawnData.Normal(1), ItemSpawnData.Normal(0));   // blue on the board, red waiting
        SetPipes(pipe);

        Item pulled = _boardManager.ExtractAnyItemOfColor(0);

        Assert.IsNotNull(pulled);
        Assert.AreEqual(0, pulled.colorID);
        Assert.IsTrue(pulled.gameObject.activeSelf);
        Assert.AreEqual(1, _pool.RequestedItemsCount, "Created now: it wasn't on the board");
        CollectionAssert.AreEqual(new[] { 1 }, TestQueues.Colors(pipe.itemsQueue), "Out of the queue: the inlet counter goes down");
        Assert.AreEqual(1, pipe.activeItems.Count, "The board doesn't move");
        Assert.AreEqual(1, pipe.activeItems[0].colorID);
    }

    [Test]
    public void Extract_FromTheQueueOfAPipeWithTube_ComesOutOfItsInlet()
    {
        // Two-cell pipe, full: two on the board, a red waiting
        var pipe = new Pipe
        {
            path = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(0, 0) },
            itemsQueue = new List<ItemSpawnData> { ItemSpawnData.Normal(1), ItemSpawnData.Normal(1), ItemSpawnData.Normal(0) },
            activeItems = new List<Item>()
        };
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("OnBoard");
            _spawned.Add(go);
            Item onBoard = go.AddComponent<Item>();
            onBoard.Init(pipe.itemsQueue[i], Color.white, pipe.path[1 - i], pipe);
            pipe.activeItems.Add(onBoard);
        }
        SetPipes(pipe);

        Item pulled = _boardManager.ExtractAnyItemOfColor(0);

        Assert.IsNotNull(pulled);
        Assert.AreEqual(_boardManager.InletWorldPosition(pipe), pulled.transform.position);
        Assert.AreEqual(2, pipe.itemsQueue.Count);
    }

    [Test]
    public void Extract_PrefersTheBoard_OverTheQueues()
    {
        Pipe withQueue = MakePipe(0, ItemSpawnData.Normal(1), ItemSpawnData.Normal(0));
        Pipe onBoard = MakePipe(2, ItemSpawnData.Normal(0));
        Item red = onBoard.activeItems[0];
        SetPipes(withQueue, onBoard);

        Item pulled = _boardManager.ExtractAnyItemOfColor(0);

        Assert.AreSame(red, pulled);
        Assert.AreEqual(2, withQueue.itemsQueue.Count, "The queue isn't touched");
        Assert.AreEqual(0, _pool.RequestedItemsCount);
    }

    [Test]
    public void Extract_InTheQueue_NormalBeforeHiddenBeforeBarrel()
    {
        Pipe pipe = MakePipe(0, ItemSpawnData.Normal(1), Barrel(0), Hidden(0), ItemSpawnData.Normal(0));
        SetPipes(pipe);

        Item first = _boardManager.ExtractAnyItemOfColor(0);
        Item second = _boardManager.ExtractAnyItemOfColor(0);
        Item third = _boardManager.ExtractAnyItemOfColor(0);

        Assert.IsTrue(first.HasKnownColor, "Normal first");
        Assert.IsTrue(second.IsHidden, "Then the hidden one");
        Assert.IsFalse(third.IsBarrel, "The barrel last, opened: its content goes to the tray");
        Assert.AreEqual(0, third.colorID);
        Assert.AreEqual(1, pipe.itemsQueue.Count);
    }

    [Test]
    public void Extract_NoneAnywhere_ReturnsNull()
    {
        SetPipes(MakePipe(0, ItemSpawnData.Normal(1), ItemSpawnData.Normal(2)));

        Assert.IsNull(_boardManager.ExtractAnyItemOfColor(0));
    }

    // ---------- Match booster ----------

    [Test]
    public void Match_TwoInTheTray_ThirdOnlyInAQueue_CompletesTheTrio()
    {
        PutInTray(colorId: 0, count: 2);
        Pipe pipe = MakePipe(0, ItemSpawnData.Normal(1), ItemSpawnData.Normal(1), ItemSpawnData.Normal(0));
        SetPipes(pipe);

        _booster.Execute();

        Assert.AreEqual(1, _pool.RequestedItemsCount, "The third red came out of the queue");
        CollectionAssert.AreEqual(new[] { 1, 1 }, TestQueues.Colors(pipe.itemsQueue));
        Assert.AreEqual(1, pipe.activeItems[0].colorID, "The blue on the board stays where it was");
        Assert.AreEqual(0, Tray.Count(i => i != null && i.colorID == 1), "Nothing else went to the tray");
    }
}
