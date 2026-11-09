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

    // ---------- Lives ----------

    private FakeLivesService WithLives(int lives)
    {
        var fake = new FakeLivesService { Lives = lives };
        _gameManager.Initialize(_fakeUserData, _fakeSceneLoader, fake);
        return fake;
    }

    [Test]
    public void StartingTheLevel_UsesALife()
    {
        FakeLivesService lives = WithLives(5);

        Assert.IsTrue(_gameManager.TakeLevelLife());

        Assert.AreEqual(4, lives.Lives);
        Assert.IsTrue(_gameManager.LifeInPlay);
    }

    [Test]
    public void TheLifeIsTakenOnlyOncePerLevel()
    {
        FakeLivesService lives = WithLives(5);

        _gameManager.TakeLevelLife();
        _gameManager.TakeLevelLife();

        Assert.AreEqual(4, lives.Lives);
    }

    [Test]
    public void WinningTheLevel_GivesTheLifeBack()
    {
        FakeLivesService lives = WithLives(5);
        _gameManager.TakeLevelLife();

        _gameManager.TrySetState(GameState.Victory);

        Assert.AreEqual(5, lives.Lives);
        Assert.AreEqual(1, lives.ReturnCalls);
        Assert.IsFalse(_gameManager.LifeInPlay);
    }

    [Test]
    public void LosingTheLevel_KeepsTheLifeSpent()
    {
        FakeLivesService lives = WithLives(5);
        _gameManager.TakeLevelLife();

        _gameManager.TrySetState(GameState.Defeat);

        Assert.AreEqual(4, lives.Lives);
        Assert.AreEqual(0, lives.ReturnCalls);
    }

    [Test]
    public void AbandoningTheLevel_KeepsTheLifeSpent()
    {
        FakeLivesService lives = WithLives(5);
        _gameManager.TakeLevelLife();

        _gameManager.AbandonLevel();

        Assert.AreEqual(4, lives.Lives);
        Assert.AreEqual(0, lives.ReturnCalls);
    }

    [Test]
    public void WinningALevelThatTookNoLife_GivesNothing()
    {
        FakeLivesService lives = WithLives(3);

        _gameManager.TrySetState(GameState.Victory);

        Assert.AreEqual(0, lives.ReturnCalls);
        Assert.AreEqual(3, lives.Lives);
    }

    [Test]
    public void StartingWithNoLives_PlaysWithoutTakingOne()
    {
        FakeLivesService lives = WithLives(0);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no lives left"));
        Assert.IsFalse(_gameManager.TakeLevelLife());

        Assert.AreEqual(0, lives.Lives);
        Assert.IsFalse(_gameManager.LifeInPlay);
    }
}
