using NUnit.Framework;
using TMPro;
using UnityEngine;

public class CurrentLanguageButtonTests
{
    private GameObject _root;
    private CurrentLanguageButton _button;
    private TextMeshProUGUI _label;
    private FakeLocalizationService _localization;
    private FakePopupService _popups;
    private LanguagePopup _popupPrefab;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("LanguageButton", typeof(RectTransform));
        _button = _root.AddComponent<CurrentLanguageButton>();
        _label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _label.transform.SetParent(_root.transform, false);
        PopupTestUtils.SetField(_button, "label", _label);

        _popupPrefab = PopupTestUtils.MakePrefab<LanguagePopup>("LanguagePopup");
        PopupTestUtils.SetField(_button, "languagePopupPrefab", _popupPrefab);

        _localization = new FakeLocalizationService();
        _popups = new FakePopupService();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_popupPrefab.gameObject);
    }

    [Test]
    public void ShowsTheLanguageOnScreen_InItsOwnName()
    {
        _localization.CurrentLanguage = "ca";

        _button.Initialize(_localization, _popups);

        Assert.AreEqual("Català", _label.text);
    }

    [Test]
    public void FollowsLanguageChanges()
    {
        _button.Initialize(_localization, _popups);

        _localization.SetLanguage("es");

        Assert.AreEqual("Español", _label.text);
    }

    [Test]
    public void Tap_OpensTheLanguagePopup()
    {
        _button.Initialize(_localization, _popups);

        _button.OnClicked();

        CollectionAssert.AreEqual(new PopupView[] { _popupPrefab }, _popups.Opened);
    }

    [Test]
    public void CurrentName_UnknownLanguage_ShowsTheCode()
    {
        Assert.AreEqual("fr", CurrentLanguageButton.CurrentName("fr", _localization.Languages));
        Assert.AreEqual(string.Empty, CurrentLanguageButton.CurrentName(null, null));
    }
}
