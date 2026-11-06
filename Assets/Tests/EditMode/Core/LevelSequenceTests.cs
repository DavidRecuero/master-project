using System;
using System.Linq;
using NUnit.Framework;

public class LevelSequenceTests
{
    private const int DesignedLevels = 8;

    [Test]
    public void ResolveLevelFile_DesignedLevel_ReturnsSameLevel()
    {
        LevelSequence sequence = new LevelSequence(DesignedLevels);

        for (int level = 1; level <= DesignedLevels; level++)
        {
            Assert.AreEqual(level, sequence.ResolveLevelFile(level));
            Assert.IsFalse(sequence.IsLoopLevel(level));
        }
    }

    [Test]
    public void ResolveLevelFile_LoopLevel_ReturnsADesignedLevel()
    {
        LevelSequence sequence = new LevelSequence(DesignedLevels);

        for (int level = DesignedLevels + 1; level <= 1000; level++)
        {
            int file = sequence.ResolveLevelFile(level);

            Assert.IsTrue(sequence.IsLoopLevel(level));
            Assert.That(file, Is.InRange(1, DesignedLevels), $"Level {level}");
        }
    }

    [Test]
    public void ResolveLevelFile_SameLevel_AlwaysReturnsSameBoard()
    {
        // Replay, back to menu, app restart, another device: every case builds a new sequence
        for (int level = 1; level <= 500; level++)
        {
            int first = new LevelSequence(DesignedLevels).ResolveLevelFile(level);
            int second = new LevelSequence(DesignedLevels).ResolveLevelFile(level);

            Assert.AreEqual(first, second, $"Level {level}");
        }
    }

    [Test]
    public void ResolveLevelFile_EachLoopCycle_PlaysEveryDesignedLevelOnce()
    {
        LevelSequence sequence = new LevelSequence(DesignedLevels);

        for (int cycle = 0; cycle < 100; cycle++)
        {
            int firstLevel = DesignedLevels + 1 + cycle * DesignedLevels;
            int[] boards = Enumerable.Range(firstLevel, DesignedLevels).Select(sequence.ResolveLevelFile).ToArray();

            CollectionAssert.AreEquivalent(Enumerable.Range(1, DesignedLevels), boards, $"Cycle {cycle}");
        }
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(15)]
    public void ResolveLevelFile_NeverSameBoardTwiceInARow(int designedLevels)
    {
        LevelSequence sequence = new LevelSequence(designedLevels);

        // Includes the jump from the last designed level to the loop and every jump between cycles
        for (int level = 2; level <= 3000; level++)
        {
            Assert.AreNotEqual(sequence.ResolveLevelFile(level - 1), sequence.ResolveLevelFile(level), $"Levels {level - 1} and {level}");
        }
    }

    [Test]
    public void ResolveLevelFile_LoopOrder_ChangesBetweenCycles()
    {
        LevelSequence sequence = new LevelSequence(DesignedLevels);

        int[] firstCycle = LoopCycle(sequence, 0);
        bool anyDifferent = Enumerable.Range(1, 10).Any(cycle => !LoopCycle(sequence, cycle).SequenceEqual(firstCycle));

        Assert.IsTrue(anyDifferent);
    }

    [Test]
    public void ResolveLevelFile_DifferentSeed_ChangesLoopOrder()
    {
        int[] defaultSeed = LoopCycle(new LevelSequence(DesignedLevels), 0);
        int[] otherSeed = LoopCycle(new LevelSequence(DesignedLevels, loopSeed: 12345), 0);

        CollectionAssert.AreNotEqual(defaultSeed, otherSeed);
    }

    /// <summary>
    /// Pins the boards of the first two loop cycles with the default seed. If this test fails, a change in
    /// LevelSequence moved the boards of players who are already in the loop: only accept it on purpose.
    /// </summary>
    [Test]
    public void ResolveLevelFile_DefaultSeed_KeepsKnownLoopBoards()
    {
        LevelSequence sequence = new LevelSequence(DesignedLevels);
        int[] expected = { 5, 3, 2, 8, 7, 4, 6, 1, 7, 2, 1, 5, 8, 3, 4, 6 }; // levels 9 to 24

        int[] actual = Enumerable.Range(DesignedLevels + 1, expected.Length).Select(sequence.ResolveLevelFile).ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    public void ResolveLevelFile_OneDesignedLevel_AlwaysReturnsIt()
    {
        LevelSequence sequence = new LevelSequence(1);

        for (int level = 1; level <= 20; level++)
        {
            Assert.AreEqual(1, sequence.ResolveLevelFile(level));
        }
    }

    [Test]
    public void ResolveLevelFile_TwoDesignedLevels_Alternate()
    {
        LevelSequence sequence = new LevelSequence(2);

        int[] boards = Enumerable.Range(1, 8).Select(sequence.ResolveLevelFile).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 1, 2, 1, 2, 1, 2 }, boards);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void ResolveLevelFile_LevelBelowOne_ReturnsFirstLevel(int level)
    {
        Assert.AreEqual(1, new LevelSequence(DesignedLevels).ResolveLevelFile(level));
    }

    [Test]
    public void Constructor_NoDesignedLevels_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSequence(0));
    }

    private static int[] LoopCycle(LevelSequence sequence, int cycle)
    {
        int firstLevel = sequence.DesignedLevels + 1 + cycle * sequence.DesignedLevels;
        return Enumerable.Range(firstLevel, sequence.DesignedLevels).Select(sequence.ResolveLevelFile).ToArray();
    }
}
