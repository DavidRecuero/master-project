using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The Settings button of the gameplay HUD and GameManager.AbandonLevel.</summary>
public class GameplaySettingsTests
{
    private GameObject _buttonGo;
    private GameplaySettingsButton _settingsButton;
    private Button _button;
    private SettingsPopup _settingsPrefab;
    private FakePopupService _popups;

    [SetUp]
    public void SetUp()
    {
        _buttonGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        _button = _buttonGo.GetComponent<Button>();
        _settingsButton = _buttonGo.AddComponent<GameplaySettingsButton>();
        _settingsPrefab = PopupTestUtils.MakePrefab<SettingsPopup>("GameplaySettingsPopup");
        PopupTestUtils.SetField(_settingsButton, "settingsPopupPrefab", _settingsPrefab);

        _popups = new FakePopupService();
        _settingsButton.Initialize(_popups);

        // EditMode doesn't run the lifecycle: call it as the player would
        Invoke(_settingsButton, "Awake");
        Invoke(_settingsButton, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        Invoke(_settingsButton, "OnDisable");   // unsubscribes from the static GameEvents
        Object.DestroyImmediate(_buttonGo);
        Object.DestroyImmediate(_settingsPrefab.gameObject);
    }

    [Test]
    public void Click_OpensTheGameplaySettings()
    {
        _settingsButton.OnClicked();

        Assert.AreEqual(1, _popups.Count);
        Assert.AreSame(_settingsPrefab, _popups.Top);
    }

    [Test]
    public void Click_WithoutPrefab_OpensNothing()
    {
        PopupTestUtils.SetField(_settingsButton, "settingsPopupPrefab", null);

        _settingsButton.OnClicked();

        Assert.AreEqual(0, _popups.Count);
    }

    [Test]
    public void Click_WithoutPopupService_DoesNotThrow()
    {
        _settingsButton.Initialize(null);

        Assert.DoesNotThrow(() => _settingsButton.OnClicked());
    }

    [TestCase(GameState.Victory)]
    [TestCase(GameState.Defeat)]
    public void OnceTheLevelEnds_TheButtonStopsWorking(GameState end)
    {
        if (end == GameState.Victory) GameEvents.TriggerLevelCleared();
        else GameEvents.TriggerLevelFailed();

        _settingsButton.OnClicked();

        Assert.IsFalse(_button.interactable);
        Assert.AreEqual(0, _popups.Count);
    }

    [Test]
    public void Click_OnTheButton_OpensTheSettings()
    {
        _button.onClick.Invoke();

        Assert.AreEqual(1, _popups.Count);
    }

    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    // ---------- GameManager.AbandonLevel ----------

    private static GameManager MakeGameManager(out FakeSceneLoader sceneLoader, out GameObject go)
    {
        go = new GameObject("GameManager");
        GameManager manager = go.AddComponent<GameManager>();
        sceneLoader = new FakeSceneLoader();
        manager.Initialize(new FakeUserDataProvider { CurrentLevel = 3 }, sceneLoader);
        return manager;
    }

    [Test]
    public void AbandonLevel_WhilePlaying_GoesBackToTheMenu()
    {
        GameManager manager = MakeGameManager(out FakeSceneLoader loader, out GameObject go);
        bool abandonedEvent = false, failedEvent = false;
        System.Action onAbandoned = () => abandonedEvent = true;
        System.Action onFailed = () => failedEvent = true;
        GameEvents.OnLevelAbandoned += onAbandoned;
        GameEvents.OnLevelFailed += onFailed;

        try
        {
            bool abandoned = manager.AbandonLevel();

            Assert.IsTrue(abandoned);
            Assert.AreEqual(GameState.Abandoned, manager.CurrentState);
            Assert.AreEqual(1, loader.LoadedSceneIndex, "Main menu");
            Assert.IsTrue(abandonedEvent);
            Assert.IsFalse(failedEvent, "No result popup or defeat sound when the player leaves");
        }
        finally
        {
            GameEvents.OnLevelAbandoned -= onAbandoned;
            GameEvents.OnLevelFailed -= onFailed;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AbandonLevel_AfterTheLevelEnded_DoesNothing()
    {
        GameManager manager = MakeGameManager(out FakeSceneLoader loader, out GameObject go);

        try
        {
            manager.TrySetState(GameState.Victory);

            Assert.IsFalse(manager.AbandonLevel());
            Assert.AreEqual(GameState.Victory, manager.CurrentState);
            Assert.AreEqual(-1, loader.LoadedSceneIndex, "Stays on the result popup");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AbandonLevel_Twice_LoadsTheMenuOnce()
    {
        GameManager manager = MakeGameManager(out FakeSceneLoader loader, out GameObject go);

        try
        {
            Assert.IsTrue(manager.AbandonLevel());
            Assert.IsFalse(manager.AbandonLevel());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
