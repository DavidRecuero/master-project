using NUnit.Framework;
using UnityEngine;

public class ComboTrackerTests
{
    [Test]
    public void Steps_GoUpInsideTheWindow_AndAreCapped()
    {
        var combo = new ComboTracker(window: 2f, maxSteps: 2, semitonesPerStep: 2f);

        combo.RegisterMatch(0f);
        Assert.AreEqual(0, combo.Step);
        combo.RegisterMatch(1f);
        Assert.AreEqual(1, combo.Step);
        combo.RegisterMatch(2f);
        Assert.AreEqual(2, combo.Step);
        combo.RegisterMatch(3f);
        Assert.AreEqual(2, combo.Step, "Capped at maxSteps");
    }

    [Test]
    public void Pitch_OneWholeTonePerStep()
    {
        var combo = new ComboTracker(semitonesPerStep: 2f);

        Assert.AreEqual(1f, combo.PitchForStep(0), 0.0001f);
        Assert.AreEqual(Mathf.Pow(2f, 2f / 12f), combo.PitchForStep(1), 0.0001f);
        Assert.AreEqual(Mathf.Pow(2f, 1f / 2f), combo.PitchForStep(3), 0.0001f);
    }

    [Test]
    public void Reset_StartsOverAtZero()
    {
        var combo = new ComboTracker(window: 2f);
        combo.RegisterMatch(0f);
        combo.RegisterMatch(1f);

        combo.Reset();

        Assert.AreEqual(1f, combo.RegisterMatch(1.5f), 0.0001f);
    }
}

public class SfxCooldownsTests
{
    [Test]
    public void SameSound_BlockedInsideTheCooldown()
    {
        var cooldowns = new SfxCooldowns();

        Assert.IsTrue(cooldowns.TryPlay(SfxId.ItemLand, 0f, 0.05f));
        Assert.IsFalse(cooldowns.TryPlay(SfxId.ItemLand, 0.02f, 0.05f));
        Assert.IsTrue(cooldowns.TryPlay(SfxId.ItemLand, 0.06f, 0.05f));
    }

    [Test]
    public void DifferentSounds_DontBlockEachOther()
    {
        var cooldowns = new SfxCooldowns();

        Assert.IsTrue(cooldowns.TryPlay(SfxId.ItemTap, 0f, 1f));
        Assert.IsTrue(cooldowns.TryPlay(SfxId.Match3, 0f, 1f));
    }

    [Test]
    public void ZeroCooldown_NeverBlocks()
    {
        var cooldowns = new SfxCooldowns();

        Assert.IsTrue(cooldowns.TryPlay(SfxId.ItemTap, 0f, 0f));
        Assert.IsTrue(cooldowns.TryPlay(SfxId.ItemTap, 0f, 0f));
    }
}

public class SoundLibraryTests
{
    private SoundLibrary _library;
    private AudioClip _a;
    private AudioClip _b;

    [SetUp]
    public void SetUp()
    {
        _library = ScriptableObject.CreateInstance<SoundLibrary>();
        _a = AudioClip.Create("A", 441, 1, 44100, false);
        _b = AudioClip.Create("B", 441, 1, 44100, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_library);
        Object.DestroyImmediate(_a);
        Object.DestroyImmediate(_b);
    }

    [Test]
    public void Get_FindsTheEntry_OrNull()
    {
        SoundEntry entry = _library.GetOrCreate(SfxId.Match3);

        Assert.AreSame(entry, _library.Get(SfxId.Match3));
        Assert.IsNull(_library.Get(SfxId.LevelWin));
        Assert.IsNull(_library.Get(SfxId.None));
    }

    [Test]
    public void GetOrCreate_DoesNotDuplicate()
    {
        _library.GetOrCreate(SfxId.ItemTap);
        _library.GetOrCreate(SfxId.ItemTap);

        Assert.AreEqual(1, _library.Entries.Count);
    }

    [Test]
    public void PickClip_ChoosesOneOfTheVariants()
    {
        var entry = new SoundEntry { id = SfxId.ItemTap, clips = new[] { _a, _b } };
        var rng = new System.Random(1);

        for (int i = 0; i < 20; i++)
        {
            AudioClip clip = SoundLibrary.PickClip(entry, rng);
            Assert.IsTrue(clip == _a || clip == _b);
        }
    }

    [Test]
    public void PickClip_NoClips_ReturnsNull()
    {
        Assert.IsNull(SoundLibrary.PickClip(new SoundEntry(), new System.Random()));
        Assert.IsNull(SoundLibrary.PickClip(null, new System.Random()));
    }

    [Test]
    public void PickPitch_StaysInsideTheRange()
    {
        var entry = new SoundEntry { pitchRange = new Vector2(0.95f, 1.05f) };
        var rng = new System.Random(3);

        for (int i = 0; i < 50; i++)
        {
            float p = SoundLibrary.PickPitch(entry, rng);
            Assert.That(p, Is.InRange(0.95f, 1.05f));
        }
    }

    [Test]
    public void PickPitch_NoRange_IsOne()
    {
        Assert.AreEqual(1f, SoundLibrary.PickPitch(new SoundEntry(), new System.Random()), 0.0001f);
    }
}

public class SoundOnEnableTests
{
    [Test]
    public void ShouldPlay_SilentDuringSceneLoad_UnlessAsked()
    {
        Assert.IsFalse(SoundOnEnable.ShouldPlay(0f, false));
        Assert.IsTrue(SoundOnEnable.ShouldPlay(0f, true));
        Assert.IsTrue(SoundOnEnable.ShouldPlay(2f, false));
    }
}
