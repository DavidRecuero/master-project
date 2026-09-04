using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    private GameObject _gameManagerGO;
    private GameManager _gameManager;
    private FakeUserDataProvider _fakeUserData;
    private FakeSceneLoader _fakeSceneLoader;

    [SetUp]
    public void SetUp()
    {
        _gameManagerGO = new GameObject();
        _gameManager = _gameManagerGO.AddComponent<GameManager>();

        _fakeUserData = new FakeUserDataProvider { CurrentLevel = 3 };
        _fakeSceneLoader = new FakeSceneLoader();

        _gameManager.Initialize(_fakeUserData, _fakeSceneLoader);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameManagerGO);
    }

    [Test]
    public void TrySetState_FromPlayingToVictory_ChangesStateAndFiresEvent()
    {
        bool eventFired = false;
        GameEvents.OnLevelCleared += () => eventFired = true;

        bool success = _gameManager.TrySetState(GameState.Victory);

        Assert.IsTrue(success);
        Assert.AreEqual(GameState.Victory, _gameManager.CurrentState);
        Assert.IsTrue(eventFired);
    }

    [Test]
    public void TrySetState_WhenNotPlaying_ReturnsFalse()
    {
        _gameManager.TrySetState(GameState.Victory);

        bool success = _gameManager.TrySetState(GameState.Defeat);

        Assert.IsFalse(success);
        Assert.AreEqual(GameState.Victory, _gameManager.CurrentState);
    }

    [Test]
    public void PlayAgain_TriggersSceneReload()
    {
        _gameManager.PlayAgain();

        Assert.IsTrue(_fakeSceneLoader.ReloadCalled);
    }

    [Test]
    public void BackToMenu_LoadsMainMenuScene()
    {
        _gameManager.BackToMenu();

        Assert.AreEqual(1, _fakeSceneLoader.LoadedSceneIndex);
    }
}