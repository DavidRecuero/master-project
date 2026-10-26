using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SceneMusicTests
{
    private GameObject _go;
    private AudioClip _clip;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("SceneMusic");
        _clip = AudioClip.Create("Track", 4410, 1, 44100, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_clip);
    }

    [Test]
    public void Start_AsksForItsTrackWithItsVolume()
    {
        var audio = new FakeAudioService();
        var sceneMusic = _go.AddComponent<SceneMusic>();
        sceneMusic.volume = 0.6f;
        sceneMusic.Initialize(audio, _clip);

        typeof(SceneMusic).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sceneMusic, null);

        Assert.AreEqual(1, audio.PlayMusicCalls);
        Assert.AreEqual(_clip, audio.LastMusic);
        Assert.AreEqual(0.6f, audio.LastMusicVolume, 0.0001f);
    }
}
