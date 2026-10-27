using NUnit.Framework;
using UnityEngine;

public class MainMenuSettingsTests
{
    private GameObject _menuGo;
    private MainMenuController _menu;
    private SettingsPopup _settingsPrefab;
    private FakePopupService _popups;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();

        _menuGo = new GameObject("MainMenuController");
        _menu = _menuGo.AddComponent<MainMenuController>();
        _settingsPrefab = PopupTestUtils.MakePrefab<SettingsPopup>("SettingsPopup");
        PopupTestUtils.SetField(_menu, "settingsPopupPrefab", _settingsPrefab);

        _popups = new FakePopupService();
        _menu.Initialize(new FakeUserDataProvider(), new FakeSceneLoader(), _popups);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerSession.Reset();
        Object.DestroyImmediate(_menuGo);
        Object.DestroyImmediate(_settingsPrefab.gameObject);
    }

    [Test]
    public void SettingsButton_OpensTheSettingsPopup()
    {
        _menu.OnSettingsButtonClicked();

        Assert.AreEqual(1, _popups.Opened.Count);
        Assert.AreSame(_settingsPrefab, _popups.Opened[0]);
    }
}
