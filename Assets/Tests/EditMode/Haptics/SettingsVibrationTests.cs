using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SettingsVibrationTests
{
    private GameObject _root;
    private GameObject _row;
    private SettingsPopup _settings;
    private Toggle _vibrationToggle;
    private FakeAudioService _audio;
    private FakeHapticsService _haptics;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("SettingsPopup", typeof(RectTransform));
        _settings = _root.AddComponent<SettingsPopup>();

        _row = new GameObject("VibrationRow", typeof(RectTransform));
        _row.transform.SetParent(_root.transform);
        _vibrationToggle = new GameObject("VibrationToggle").AddComponent<Toggle>();
        _vibrationToggle.transform.SetParent(_row.transform);

        PopupTestUtils.SetField(_settings, "vibrationToggle", _vibrationToggle);
        PopupTestUtils.SetField(_settings, "vibrationRow", _row);

        _audio = new FakeAudioService();
        _haptics = new FakeHapticsService();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Initialize_ShowsTheSavedVibrationState()
    {
        _haptics.Enabled = false;

        _settings.Initialize(_audio, haptics: _haptics);

        Assert.IsFalse(_vibrationToggle.isOn);
    }

    [Test]
    public void Initialize_DoesNotVibrateNorWriteTheStateBack()
    {
        _haptics.Enabled = true;

        _settings.Initialize(_audio, haptics: _haptics);

        Assert.AreEqual(0, _haptics.Played.Count, "Showing the state must not count as the player turning it on");
        Assert.IsTrue(_haptics.Enabled);
    }

    [Test]
    public void Toggle_Off_TurnsVibrationOffSilently()
    {
        _settings.Initialize(_audio, haptics: _haptics);

        _settings.OnVibrationToggled(false);

        Assert.IsFalse(_haptics.Enabled);
        Assert.AreEqual(0, _haptics.Played.Count);
    }

    [Test]
    public void Toggle_On_GivesAPulse()
    {
        _haptics.Enabled = false;
        _settings.Initialize(_audio, haptics: _haptics);

        _settings.OnVibrationToggled(true);

        Assert.IsTrue(_haptics.Enabled);
        Assert.AreEqual(HapticType.MediumImpact, _haptics.Last);
    }

    [Test]
    public void Toggle_DoesNotTouchTheAudio()
    {
        _settings.Initialize(_audio, haptics: _haptics);

        _settings.OnVibrationToggled(false);

        Assert.IsTrue(_audio.SfxEnabled);
        Assert.IsTrue(_audio.MusicEnabled);
    }

    [Test]
    public void DeviceWithoutVibrator_HidesTheRow()
    {
        _haptics.IsSupported = false;

        _settings.Initialize(_audio, haptics: _haptics);

        Assert.IsFalse(_row.activeSelf);
    }

    [Test]
    public void DeviceWithVibrator_ShowsTheRow()
    {
        _row.SetActive(false);
        _haptics.IsSupported = true;

        _settings.Initialize(_audio, haptics: _haptics);

        Assert.IsTrue(_row.activeSelf);
    }
}
