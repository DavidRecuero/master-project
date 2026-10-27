using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupTests
{
    private GameObject _root;
    private SettingsPopup _settings;
    private Toggle _musicToggle;
    private Toggle _sfxToggle;
    private FakeAudioService _audio;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("SettingsPopup", typeof(RectTransform));
        _settings = _root.AddComponent<SettingsPopup>();

        _musicToggle = new GameObject("MusicToggle").AddComponent<Toggle>();
        _sfxToggle = new GameObject("SfxToggle").AddComponent<Toggle>();
        _musicToggle.transform.SetParent(_root.transform);
        _sfxToggle.transform.SetParent(_root.transform);

        PopupTestUtils.SetField(_settings, "musicToggle", _musicToggle);
        PopupTestUtils.SetField(_settings, "sfxToggle", _sfxToggle);

        _audio = new FakeAudioService();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Initialize_ShowsTheSavedAudioState()
    {
        _audio.MusicEnabled = false;
        _audio.SfxEnabled = true;

        _settings.Initialize(_audio);

        Assert.IsFalse(_musicToggle.isOn);
        Assert.IsTrue(_sfxToggle.isOn);
    }

    [Test]
    public void Initialize_DoesNotWriteTheStateBack()
    {
        _audio.MusicEnabled = false;
        _audio.SfxEnabled = false;

        _settings.Initialize(_audio);

        Assert.AreEqual(0, _audio.Sfx.Count, "Showing the state must not count as the player turning SFX on");
        Assert.IsFalse(_audio.MusicEnabled);
        Assert.IsFalse(_audio.SfxEnabled);
    }

    [Test]
    public void MusicToggle_TurnsTheMusicOnAndOff()
    {
        _settings.Initialize(_audio);

        _settings.OnMusicToggled(false);
        Assert.IsFalse(_audio.MusicEnabled);

        _settings.OnMusicToggled(true);
        Assert.IsTrue(_audio.MusicEnabled);
    }

    [Test]
    public void SfxToggle_Off_IsSilent()
    {
        _settings.Initialize(_audio);

        _settings.OnSfxToggled(false);

        Assert.IsFalse(_audio.SfxEnabled);
        Assert.AreEqual(0, _audio.Sfx.Count);
    }

    [Test]
    public void SfxToggle_On_PlaysAClick()
    {
        _audio.SfxEnabled = false;
        _settings.Initialize(_audio);

        _settings.OnSfxToggled(true);

        Assert.IsTrue(_audio.SfxEnabled);
        Assert.AreEqual(SfxId.ButtonClick, _audio.Last);
    }

    [Test]
    public void CreditsButton_OpensTheCreditsOnTop()
    {
        // Opened through a real service, so the settings popup knows who opened it
        var serviceGo = new GameObject("PopupService");
        var service = serviceGo.AddComponent<PopupService>();
        CreditsPopup creditsPrefab = PopupTestUtils.MakePrefab<CreditsPopup>("CreditsPopup");
        PopupTestUtils.SetField(_settings, "creditsPopupPrefab", creditsPrefab);

        try
        {
            SettingsPopup settings = service.Open(_settings);
            settings.Initialize(_audio);

            settings.OnCreditsClicked();

            Assert.AreEqual(2, service.Count);
            Assert.IsInstanceOf<CreditsPopup>(service.Top);
        }
        finally
        {
            Object.DestroyImmediate(serviceGo);
            Object.DestroyImmediate(creditsPrefab.gameObject);
        }
    }
}
