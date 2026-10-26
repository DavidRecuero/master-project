using NUnit.Framework;
using UnityEngine;

public class MusicMixerTests
{
    private MusicMixer _mixer;
    private AudioClip _menu;
    private AudioClip _level;

    [SetUp]
    public void SetUp()
    {
        _mixer = new MusicMixer { CrossfadeDuration = 1f, DuckLevel = 0.25f, DuckFade = 0.25f };
        _menu = AudioClip.Create("Menu", 4410, 1, 44100, false);
        _level = AudioClip.Create("Level", 4410, 1, 44100, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_menu);
        Object.DestroyImmediate(_level);
    }

    [Test]
    public void Request_NullClip_IsIgnored()
    {
        Assert.AreEqual(MusicMixer.RequestResult.Ignored, _mixer.Request(null, 1f));
    }

    [Test]
    public void Request_NewClip_StartsSilentAndFadesIn()
    {
        Assert.AreEqual(MusicMixer.RequestResult.Started, _mixer.Request(_menu, 0.8f));
        Assert.AreEqual(0f, _mixer.VolumeOf(_mixer.ActiveIndex, 1f), 0.0001f);

        _mixer.Tick(0.5f, 0.5f);
        Assert.AreEqual(0.4f, _mixer.VolumeOf(_mixer.ActiveIndex, 1f), 0.0001f);

        _mixer.Tick(1.0f, 0.5f);
        Assert.AreEqual(0.8f, _mixer.VolumeOf(_mixer.ActiveIndex, 1f), 0.0001f);
    }

    [Test]
    public void Request_SameClipAgain_ContinuesWithoutRestart()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);
        int slot = _mixer.ActiveIndex;

        Assert.AreEqual(MusicMixer.RequestResult.Continued, _mixer.Request(_menu, 0.5f));
        Assert.AreEqual(slot, _mixer.ActiveIndex);
        Assert.AreEqual(0.5f, _mixer.VolumeOf(slot, 1f), 0.0001f);
    }

    [Test]
    public void Request_DifferentClip_CrossfadesBetweenSlots()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);
        int menuSlot = _mixer.ActiveIndex;

        _mixer.Request(_level, 1f);
        int levelSlot = _mixer.ActiveIndex;
        Assert.AreNotEqual(menuSlot, levelSlot);

        _mixer.Tick(1.5f, 0.5f);
        Assert.AreEqual(0.5f, _mixer.VolumeOf(menuSlot, 1f), 0.0001f);
        Assert.AreEqual(0.5f, _mixer.VolumeOf(levelSlot, 1f), 0.0001f);

        _mixer.Tick(2f, 0.5f);
        Assert.IsTrue(_mixer.IsFinished(menuSlot), "The old track should be ready to stop once faded out");
        Assert.IsFalse(_mixer.IsFinished(levelSlot));
    }

    [Test]
    public void Request_TrackStillFadingOut_IsBroughtBackInsteadOfRestarted()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);
        _mixer.Request(_level, 1f);
        _mixer.Tick(1.3f, 0.3f);   // Menu at 0.7 on its way out

        Assert.AreEqual(MusicMixer.RequestResult.Continued, _mixer.Request(_menu, 1f));
        Assert.AreEqual(_menu, _mixer.Active.Clip);
        Assert.AreEqual(0.7f, _mixer.Active.Fade, 0.0001f, "It keeps its current level, no jump to silence");
    }

    [Test]
    public void Stop_FadesEverythingOut()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);

        _mixer.Stop();
        _mixer.Tick(2f, 1f);

        Assert.AreEqual(0f, _mixer.VolumeOf(_mixer.ActiveIndex, 1f), 0.0001f);
        Assert.IsTrue(_mixer.IsFinished(_mixer.ActiveIndex));
    }

    [Test]
    public void Duck_LowersMusicAndRecoversAfterwards()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);

        _mixer.RequestDuck(1f, 2f);           // Ducked until t = 3
        _mixer.Tick(1.25f, 0.25f);
        Assert.AreEqual(0.25f, _mixer.Duck, 0.0001f);

        _mixer.Tick(3.25f, 0.25f);             // Past the duck: back up
        Assert.AreEqual(1f, _mixer.Duck, 0.0001f);
    }

    [Test]
    public void VolumeOf_AppliesPlayerMusicVolume()
    {
        _mixer.Request(_menu, 1f);
        _mixer.Tick(1f, 1f);

        Assert.AreEqual(0.3f, _mixer.VolumeOf(_mixer.ActiveIndex, 0.3f), 0.0001f);
        Assert.AreEqual(0f, _mixer.VolumeOf(_mixer.ActiveIndex, 0f), 0.0001f);
    }
}
