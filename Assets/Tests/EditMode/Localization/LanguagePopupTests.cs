using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class LanguagePopupTests
{
    private GameObject _optionPrefabGo;
    private LanguageOptionView _optionPrefab;
    private LanguagePopup _popup;
    private FakeLocalizationService _localization;

    [SetUp]
    public void SetUp()
    {
        // Option "prefab": the view and its label
        _optionPrefabGo = new GameObject("LanguageOption", typeof(RectTransform));
        _optionPrefab = _optionPrefabGo.AddComponent<LanguageOptionView>();
        var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(_optionPrefabGo.transform, false);
        var mark = new GameObject("Selected", typeof(RectTransform));
        mark.transform.SetParent(_optionPrefabGo.transform, false);
        PopupTestUtils.SetField(_optionPrefab, "label", label);
        PopupTestUtils.SetField(_optionPrefab, "selectedMark", mark);

        // The popup, used directly (not through the service, so it doesn't look for the real localization)
        _popup = PopupTestUtils.MakePrefab<LanguagePopup>("LanguagePopup");
        var container = new GameObject("Options", typeof(RectTransform));
        container.transform.SetParent(_popup.transform, false);
        PopupTestUtils.SetField(_popup, "optionsContainer", container.GetComponent<RectTransform>());
        PopupTestUtils.SetField(_popup, "optionPrefab", _optionPrefab);

        _localization = new FakeLocalizationService();
    }

    [TearDown]
    public void TearDown()
    {
        if (_popup != null) Object.DestroyImmediate(_popup.gameObject);
        Object.DestroyImmediate(_optionPrefabGo);
    }

    [Test]
    public void ListsEveryLanguageInItsOwnName()
    {
        _popup.Initialize(_localization);

        CollectionAssert.AreEqual(new[] { "en", "es", "ca" }, _popup.Options.Select(o => o.Code));
        CollectionAssert.AreEqual(new[] { "English", "Español", "Català" },
            _popup.Options.Select(o => o.GetComponentInChildren<TMP_Text>().text));
    }

    [Test]
    public void MarksTheLanguageOnScreen_EvenWhenItComesFromTheDevice()
    {
        _localization.FollowsDevice = true;
        _localization.CurrentLanguage = "es";

        _popup.Initialize(_localization);

        CollectionAssert.AreEqual(new[] { false, true, false }, _popup.Options.Select(o => o.IsSelected));
    }

    [Test]
    public void PickingAnotherLanguage_SetsItAndCloses()
    {
        _popup.Initialize(_localization);

        _popup.Options[2].Click();

        CollectionAssert.AreEqual(new[] { "ca" }, _localization.SetLanguageCalls);
        Assert.IsFalse(_localization.FollowsDevice, "It wins over the device language from now on");
        Assert.IsTrue(_popup == null, "The popup closes");
    }

    [Test]
    public void PickingTheLanguageOnScreen_JustCloses_AndKeepsFollowingTheDevice()
    {
        _localization.CurrentLanguage = "en";
        _popup.Initialize(_localization);

        _popup.Options[0].Click();

        Assert.IsEmpty(_localization.SetLanguageCalls);
        Assert.IsTrue(_localization.FollowsDevice);
        Assert.IsTrue(_popup == null);
    }

    [Test]
    public void Rebuilding_DoesNotDuplicateTheRows()
    {
        _popup.Initialize(_localization);
        _popup.Initialize(_localization);

        Assert.AreEqual(3, _popup.Options.Count);
    }
}
