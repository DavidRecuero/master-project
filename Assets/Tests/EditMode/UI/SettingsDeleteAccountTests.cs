using NUnit.Framework;
using UnityEngine;

/// <summary>The "Delete account" button of Settings only opens the confirmation popup on top.</summary>
public class SettingsDeleteAccountTests
{
    private GameObject _root;
    private SettingsPopup _settings;
    private GameObject _serviceGo;
    private PopupService _service;
    private DeleteAccountPopup _confirmPrefab;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("SettingsPopup", typeof(RectTransform));
        _settings = _root.AddComponent<SettingsPopup>();

        // A real service, so the settings popup knows who opened it
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();
        _confirmPrefab = PopupTestUtils.MakePrefab<DeleteAccountPopup>("DeleteAccountPopup");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_confirmPrefab.gameObject);
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void DeleteAccountButton_OpensTheConfirmationOnTop()
    {
        PopupTestUtils.SetField(_settings, "deleteAccountPopupPrefab", _confirmPrefab);
        SettingsPopup settings = _service.Open(_settings);
        settings.Initialize(new FakeAudioService());

        settings.OnDeleteAccountClicked();

        Assert.AreEqual(2, _service.Count);
        Assert.IsInstanceOf<DeleteAccountPopup>(_service.Top);
    }

    [Test]
    public void DeleteAccountButton_WithoutPrefab_OpensNothing()
    {
        SettingsPopup settings = _service.Open(_settings);
        settings.Initialize(new FakeAudioService());

        settings.OnDeleteAccountClicked();

        Assert.AreEqual(1, _service.Count);
    }

    [Test]
    public void TheConfirmation_WithTheCloudOff_OpensOffline()
    {
        BackendServices.Reset();
        PopupTestUtils.SetField(_settings, "deleteAccountPopupPrefab", _confirmPrefab);
        SettingsPopup settings = _service.Open(_settings);
        settings.Initialize(new FakeAudioService());

        settings.OnDeleteAccountClicked();

        var confirm = (DeleteAccountPopup)_service.Top;
        Assert.AreEqual(DeleteAccountPopup.State.Offline, confirm.CurrentState, "Without a cloud session nothing can be deleted");
    }
}
