using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The "Abandon level?" confirmation: confirm leaves the level, keep playing goes back to it.</summary>
public class AbandonLevelPopupTests
{
    private class FakeLevelAbandoner : ILevelAbandoner
    {
        public bool Result = true;
        public int Calls;

        public bool AbandonLevel()
        {
            Calls++;
            return Result;
        }
    }

    private GameObject _serviceGo;
    private PopupService _service;
    private SettingsPopup _settingsPrefab;
    private AbandonLevelPopup _confirmPrefab;
    private FakeLevelAbandoner _abandoner;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();
        _settingsPrefab = PopupTestUtils.MakePrefab<SettingsPopup>("GameplaySettingsPopup");
        _confirmPrefab = PopupTestUtils.MakePrefab<AbandonLevelPopup>("AbandonLevelPopup");
        PopupTestUtils.SetField(_settingsPrefab, "abandonLevelPopupPrefab", _confirmPrefab);
        _abandoner = new FakeLevelAbandoner();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_settingsPrefab.gameObject);
        Object.DestroyImmediate(_confirmPrefab.gameObject);
    }

    /// <summary>Settings open, then the confirmation on top, as the player would get there.</summary>
    private AbandonLevelPopup OpenConfirmation()
    {
        SettingsPopup settings = _service.Open(_settingsPrefab);
        settings.Initialize(new FakeAudioService());
        settings.OnAbandonClicked();

        var confirm = (AbandonLevelPopup)_service.Top;
        confirm.Initialize(_abandoner);
        return confirm;
    }

    [Test]
    public void AbandonButton_OpensTheConfirmationOnTopOfSettings()
    {
        OpenConfirmation();

        Assert.AreEqual(2, _service.Count);
        Assert.IsInstanceOf<AbandonLevelPopup>(_service.Top);
        Assert.AreEqual(0, _abandoner.Calls, "Opening the confirmation abandons nothing");
    }

    [Test]
    public void AbandonButton_WithoutPrefab_OpensNothing()
    {
        PopupTestUtils.SetField(_settingsPrefab, "abandonLevelPopupPrefab", null);
        SettingsPopup settings = _service.Open(_settingsPrefab);
        settings.Initialize(new FakeAudioService());

        settings.OnAbandonClicked();

        Assert.AreEqual(1, _service.Count);
    }

    [Test]
    public void Confirm_AbandonsTheLevel_AndCantBeClosedAnymore()
    {
        AbandonLevelPopup confirm = OpenConfirmation();

        confirm.OnConfirmClicked();

        Assert.AreEqual(1, _abandoner.Calls);
        Assert.IsTrue(confirm.Abandoned);
        Assert.IsFalse(confirm.CanClose, "The menu is loading: nothing to go back to");
        Assert.IsTrue(_service.HandleBack(), "Back is handled (the popup stays)");
        Assert.AreEqual(2, _service.Count);
    }

    [Test]
    public void Confirm_TappedTwice_AbandonsOnce()
    {
        AbandonLevelPopup confirm = OpenConfirmation();

        confirm.OnConfirmClicked();
        confirm.OnConfirmClicked();

        Assert.AreEqual(1, _abandoner.Calls);
    }

    [Test]
    public void Confirm_WhenTheLevelHadAlreadyEnded_ClosesEverything()
    {
        _abandoner.Result = false;
        AbandonLevelPopup confirm = OpenConfirmation();

        confirm.OnConfirmClicked();

        Assert.IsFalse(confirm.Abandoned);
        Assert.AreEqual(0, _service.Count, "Back to the game, where the result popup is waiting");
    }

    [Test]
    public void KeepPlaying_ClosesTheConfirmationAndSettings()
    {
        AbandonLevelPopup confirm = OpenConfirmation();

        confirm.OnKeepPlayingClicked();

        Assert.AreEqual(0, _abandoner.Calls);
        Assert.AreEqual(0, _service.Count, "Straight back to the level in progress");
    }

    [Test]
    public void BackButton_ClosesOnlyTheConfirmation()
    {
        OpenConfirmation();

        _service.HandleBack();

        Assert.AreEqual(1, _service.Count);
        Assert.IsInstanceOf<SettingsPopup>(_service.Top);
        Assert.AreEqual(0, _abandoner.Calls);
    }

    [Test]
    public void Buttons_AreWiredFromTheInspector()
    {
        var confirmButton = new GameObject("Confirm", typeof(RectTransform)).AddComponent<Button>();
        var keepButton = new GameObject("Keep", typeof(RectTransform)).AddComponent<Button>();
        confirmButton.transform.SetParent(_confirmPrefab.transform, false);
        keepButton.transform.SetParent(_confirmPrefab.transform, false);
        PopupTestUtils.SetField(_confirmPrefab, "confirmButton", confirmButton);
        PopupTestUtils.SetField(_confirmPrefab, "keepPlayingButton", keepButton);

        AbandonLevelPopup confirm = OpenConfirmation();
        // EditMode doesn't run Awake, where the buttons are wired
        typeof(AbandonLevelPopup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(confirm, null);
        Button openConfirm = confirm.transform.Find("Confirm").GetComponent<Button>();

        openConfirm.onClick.Invoke();

        Assert.AreEqual(1, _abandoner.Calls);
        Assert.IsFalse(openConfirm.interactable, "No second tap while the menu loads");
    }
}
