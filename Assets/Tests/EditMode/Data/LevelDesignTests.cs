using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks every designed level (Resources/level_XXX.json) against the rules the game relies on, so a broken
/// or unwinnable level never reaches a build: shape of the pipes, exits, inlets, colors, matches of three,
/// barrels and fog.
/// </summary>
public class LevelDesignTests
{
    private const int MatchSize = 3;
    private const string PalettePath = "Assets/_Project/Data/ItemPalette.asset";

    private static IEnumerable<int> LevelNumbers()
    {
        int count = new LevelLoader().CountLevels();
        return Enumerable.Range(1, count);
    }

    private static LevelData Load(int number)
    {
        LevelData level = new LevelLoader().LoadLevel(number);
        Assert.IsNotNull(level, $"Level {number} doesn't load");
        return level;
    }

    private static List<Vector2Int> Path(PipeData pipe) => pipe.path.Select(c => new Vector2Int(c.x, c.y)).ToList();

    private static bool Inside(LevelData level, Vector2Int c) => c.x >= 0 && c.x < level.width && c.y >= 0 && c.y < level.height;

    [Test]
    public void EveryLevel_HasItsOwnNumber([ValueSource(nameof(LevelNumbers))] int number)
    {
        Assert.AreEqual(number, Load(number).levelNumber);
    }

    [Test]
    public void Pipes_AreContinuousTubesInsideTheBoard([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        Assert.IsNotEmpty(level.pipes);

        foreach (PipeData pipe in level.pipes)
        {
            List<Vector2Int> path = Path(pipe);
            Assert.GreaterOrEqual(path.Count, 2, $"Level {number}, layer {pipe.layer}: a pipe needs at least two cells");
            Assert.AreEqual(path.Count, path.Distinct().Count(), $"Level {number}, layer {pipe.layer}: the pipe goes through a cell twice");

            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsTrue(Inside(level, path[i]), $"Level {number}, layer {pipe.layer}: {path[i]} is outside the board");
                if (i > 0)
                {
                    Vector2Int step = path[i] - path[i - 1];
                    Assert.AreEqual(1, Mathf.Abs(step.x) + Mathf.Abs(step.y), $"Level {number}, layer {pipe.layer}: gap between {path[i - 1]} and {path[i]}");
                }
            }

            // The inlet mouth (and its counter) sits just outside the board
            Vector2Int inlet = PipePieceResolver.VirtualInletCell(path);
            Assert.IsFalse(Inside(level, inlet), $"Level {number}, layer {pipe.layer}: the inlet points into the board");
        }

        Assert.AreEqual(level.pipes.Count, level.pipes.Select(p => p.layer).Distinct().Count(), $"Level {number}: two pipes share a layer");
    }

    [Test]
    public void Exits_AreOnTheBottomRow_AndNeverShared([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        List<Vector2Int> exits = level.pipes.Select(p => Path(p).Last()).ToList();

        Assert.AreEqual(exits.Count, exits.Distinct().Count(), $"Level {number}: two pipes end on the same exit");
        foreach (Vector2Int exit in exits) Assert.AreEqual(0, exit.y, $"Level {number}: exit {exit} is not on the bottom row");

        // No pipe crosses another pipe's exit (the platform must stay tappable)
        foreach (PipeData pipe in level.pipes)
        {
            List<Vector2Int> path = Path(pipe);
            foreach (Vector2Int cell in path.Take(path.Count - 1))
                CollectionAssert.DoesNotContain(exits, cell, $"Level {number}, layer {pipe.layer}: the pipe runs over an exit at {cell}");
        }
    }

    [Test]
    public void Cells_AreSharedByTwoPipesAtMost([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        var uses = level.pipes.SelectMany(Path).GroupBy(c => c);

        foreach (var cell in uses)
            Assert.LessOrEqual(cell.Count(), 2, $"Level {number}: {cell.Count()} pipes cross at {cell.Key}");
    }

    [Test]
    public void EveryColorExists_AndCompletesMatchesOfThree([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        var palette = AssetDatabase.LoadAssetAtPath<ItemPalette>(PalettePath);
        Assert.IsNotNull(palette, PalettePath);

        var counts = new Dictionary<int, int>();
        foreach (PipeData pipe in level.pipes)
        {
            List<ItemSpawnData> items = pipe.BuildSpawnQueue();
            Assert.IsNotEmpty(items, $"Level {number}, layer {pipe.layer}: empty pipe");

            foreach (ItemSpawnData item in items)
            {
                Assert.IsTrue(palette.HasColor(item.colorId), $"Level {number}: color {item.colorId} is not in the palette");
                counts[item.colorId] = counts.TryGetValue(item.colorId, out int n) ? n + 1 : 1;
            }
        }

        foreach (var color in counts)
            Assert.AreEqual(0, color.Value % MatchSize, $"Level {number}: color {color.Key} has {color.Value} items, not a multiple of {MatchSize}");
    }

    [Test]
    public void Barrels_HaveNeighbors_AndNeverBlockEachOther([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        List<int> exitColumns = level.pipes.Select(p => Path(p).Last().x).ToList();
        var barrelPipes = new List<int>();

        for (int i = 0; i < level.pipes.Count; i++)
        {
            List<ItemSpawnData> items = level.pipes[i].BuildSpawnQueue();
            if (!items.Any(it => it.type == ItemType.Barrel)) continue;

            barrelPipes.Add(i);
            Assert.IsTrue(items.Where(it => it.type == ItemType.Barrel).All(it => it.lives >= 0), $"Level {number}: negative barrel lives");
            Assert.IsTrue(exitColumns.Where((x, j) => j != i).Any(x => Mathf.Abs(x - exitColumns[i]) == 1),
                $"Level {number}, layer {level.pipes[i].layer}: a barrel needs a neighbor pipe to be broken");
        }

        // Two barrels side by side at the exits could wait for each other forever (no move left)
        foreach (int a in barrelPipes)
        foreach (int b in barrelPipes)
            if (a < b) Assert.AreNotEqual(1, Mathf.Abs(exitColumns[a] - exitColumns[b]), $"Level {number}: pipes with barrels are neighbors");
    }

    [Test]
    public void Fog_IsOneCloudOverThePipes_AwayFromTheExits([ValueSource(nameof(LevelNumbers))] int number)
    {
        LevelData level = Load(number);
        if (level.fog == null || level.fog.Count == 0) return;

        List<Vector2Int> fog = level.fog.Select(c => new Vector2Int(c.x, c.y)).ToList();
        HashSet<Vector2Int> pipeCells = new HashSet<Vector2Int>(level.pipes.SelectMany(Path));
        HashSet<Vector2Int> exits = new HashSet<Vector2Int>(level.pipes.Select(p => Path(p).Last()));

        Assert.AreEqual(fog.Count, fog.Distinct().Count(), $"Level {number}: repeated fog cells");
        Assert.IsTrue(FogMover.IsConnected(fog), $"Level {number}: the fog is not one connected cloud");
        foreach (Vector2Int cell in fog)
        {
            Assert.IsTrue(Inside(level, cell), $"Level {number}: fog {cell} outside the board");
            Assert.IsTrue(pipeCells.Contains(cell), $"Level {number}: fog {cell} is not over a pipe");
            Assert.IsFalse(exits.Contains(cell), $"Level {number}: fog {cell} covers an exit");
        }
    }
}
