using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// SceneMusic asks the music library for its track by name and plays it when it arrives: right away, later (the
/// music pack still downloading after install), or never if the player already left the scene.
/// </summary>
public class SceneMusicTests
{
    private GameObject _go;
    private AudioClip _clip;
    private FakeMusicLibrary _library;
    private FakeAudioService _audio;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("SceneMusic");
        _clip = AudioClip.Create("Track", 4410, 1, 44100, false);
        _library = new FakeMusicLibrary();
        _audio = new FakeAudioService();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_clip);
    }

    private SceneMusic CreateAndStart(string track = "pleasant_creek", float volume = 0.6f)
    {
        var sceneMusic = _go.AddComponent<SceneMusic>();
        sceneMusic.volume = volume;
        sceneMusic.Initialize(_audio, _library, track);
        typeof(SceneMusic).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sceneMusic, null);
        return sceneMusic;
    }

    [Test]
    public void Start_AsksForItsTrack_AndPlaysItWithItsVolume()
    {
        _library.Available["pleasant_creek"] = _clip;

        CreateAndStart();

        CollectionAssert.AreEqual(new[] { "pleasant_creek" }, _library.Requested);
        Assert.AreEqual(1, _audio.PlayMusicCalls);
        Assert.AreEqual(_clip, _audio.LastMusic);
        Assert.AreEqual(0.6f, _audio.LastMusicVolume, 0.0001f);
    }

    [Test]
    public void SongStillDownloading_PlaysWhenItArrives()
    {
        CreateAndStart();
        Assert.AreEqual(0, _audio.PlayMusicCalls, "Silent until the pack lands");

        _library.Deliver("pleasant_creek", _clip);

        Assert.AreEqual(1, _audio.PlayMusicCalls);
        Assert.AreEqual(_clip, _audio.LastMusic);
    }

    [Test]
    public void SongArrivesAfterLeavingTheScene_IsNotPlayed()
    {
        CreateAndStart();

        Object.DestroyImmediate(_go);
        _library.Deliver("pleasant_creek", _clip);

        Assert.AreEqual(0, _audio.PlayMusicCalls);
    }

    [Test]
    public void NoTrack_AsksForNothing()
    {
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No track set"));

        CreateAndStart(track: null);

        CollectionAssert.IsEmpty(_library.Requested);
    }
}

/// <summary>A music library that has some songs now and delivers the rest when the test says.</summary>
public class FakeMusicLibrary : IMusicLibrary
{
    public readonly Dictionary<string, AudioClip> Available = new Dictionary<string, AudioClip>();
    public readonly List<string> Requested = new List<string>();
    private readonly Dictionary<string, List<Action<AudioClip>>> _pending = new Dictionary<string, List<Action<AudioClip>>>();

    public void Request(string track, Action<AudioClip> onReady)
    {
        Requested.Add(track);
        if (Available.TryGetValue(track, out AudioClip clip))
        {
            onReady(clip);
            return;
        }

        if (!_pending.ContainsKey(track)) _pending[track] = new List<Action<AudioClip>>();
        _pending[track].Add(onReady);
    }

    public void Deliver(string track, AudioClip clip)
    {
        Available[track] = clip;
        if (!_pending.TryGetValue(track, out List<Action<AudioClip>> callbacks)) return;

        _pending.Remove(track);
        foreach (Action<AudioClip> callback in callbacks) callback(clip);
    }
}
