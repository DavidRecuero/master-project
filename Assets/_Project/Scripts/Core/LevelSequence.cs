using System;

/// <summary>
/// Maps the player's level number to the level file that is played.
/// <para>
/// Designed levels are played in order. After the last one, the game loops over all of them
/// (the "level loop"). The order of the loop is a seeded shuffle bag:
/// each cycle plays every designed level once, in a shuffled order that depends only on the cycle number,
/// and the same board is never played twice in a row, not even between two cycles.
/// </para>
/// <para>
/// The mapping is pure and deterministic: level X always loads the same board after a replay, after going
/// back to the menu, after restarting the app, on another device or after a cloud restore. Nothing is saved.
/// </para>
/// </summary>
public sealed class LevelSequence
{
    /// <summary>
    /// Seed of the loop order. Changing it changes the board of every loop level, also for players who are
    /// in the middle of one, so only change it on purpose (a candidate for Remote Config).
    /// </summary>
    public const int DefaultLoopSeed = 20261106;

    private readonly int _designedLevels;
    private readonly int _loopSeed;

    public LevelSequence(int designedLevels, int loopSeed = DefaultLoopSeed)
    {
        if (designedLevels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(designedLevels), designedLevels, "At least one designed level is needed.");
        }

        _designedLevels = designedLevels;
        _loopSeed = loopSeed;
    }

    public int DesignedLevels => _designedLevels;

    /// <summary>True when the level is past the last designed level, so its board comes from the loop.</summary>
    public bool IsLoopLevel(int playerLevel) => playerLevel > _designedLevels;

    /// <summary>Number of the level file (level_XXX.json) to play for this player level.</summary>
    public int ResolveLevelFile(int playerLevel)
    {
        if (playerLevel < 1) playerLevel = 1;
        if (playerLevel <= _designedLevels) return playerLevel;

        int loopIndex = playerLevel - _designedLevels - 1; // 0 = first loop level
        int cycle = loopIndex / _designedLevels;
        int position = loopIndex % _designedLevels;

        // Too few levels to shuffle: one level repeats, two levels alternate (1, 2, 1, 2...)
        if (_designedLevels <= 2) return position + 1;

        int[] order = CycleOrder(cycle);

        // The board played just before this cycle: the last designed level, or the last one of the previous cycle.
        // With 3+ levels the fix below only touches positions 0 and 1, so the last position is never affected
        // and the previous cycle can be read without going further back.
        int previousBoard = cycle == 0 ? _designedLevels : CycleOrder(cycle - 1)[_designedLevels - 1];
        if (order[0] == previousBoard) Swap(order, 0, 1);

        return order[position];
    }

    // Shuffled designed levels (1..N) for one cycle of the loop: Fisher-Yates with our own PRNG.
    // UnityEngine.Random and System.Random are not used on purpose: their algorithm could change with an
    // engine or runtime update and that would silently change the boards of the loop.
    private int[] CycleOrder(int cycle)
    {
        int[] order = new int[_designedLevels];
        for (int i = 0; i < order.Length; i++) order[i] = i + 1;

        uint state = InitialState((uint)_loopSeed, (uint)cycle);

        for (int i = order.Length - 1; i > 0; i--)
        {
            state = NextState(state);
            int j = (int)(state % (uint)(i + 1));
            Swap(order, i, j);
        }

        return order;
    }

    // Mixes the seed and the cycle into a well spread 32-bit state (murmur3-style finalizer)
    private static uint InitialState(uint seed, uint cycle)
    {
        unchecked
        {
            uint h = seed ^ (cycle * 0x9E3779B9u);
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h == 0 ? 0x6D2B79F5u : h; // xorshift never leaves 0
        }
    }

    // xorshift32
    private static uint NextState(uint x)
    {
        unchecked
        {
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return x;
        }
    }

    private static void Swap(int[] array, int a, int b)
    {
        (array[a], array[b]) = (array[b], array[a]);
    }
}
