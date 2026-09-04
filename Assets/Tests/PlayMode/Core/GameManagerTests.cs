using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTests
{
    private GameObject gameManagerGO;
    private GameManager gameManager;

    [SetUp]
    public void SetUp()
    {
        gameManagerGO = new GameObject();
        gameManager = gameManagerGO.AddComponent<GameManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(gameManagerGO);
    }

    [Test]
    public void TrySetState_FromPlayingToVictory_ReturnsTrueAndChangesState()
    {
        Assert.AreEqual(GameState.Playing, gameManager.CurrentState);

        bool result = gameManager.TrySetState(GameState.Victory);

        Assert.IsTrue(result);
        Assert.AreEqual(GameState.Victory, gameManager.CurrentState);
    }

    [Test]
    public void TrySetState_WhenNotPlaying_ReturnsFalseAndDoesNotChangeState()
    {
        gameManager.TrySetState(GameState.Victory); 

        // Going from Victory to Defeat should fail
        bool result = gameManager.TrySetState(GameState.Defeat);

        Assert.IsFalse(result);
        Assert.AreEqual(GameState.Victory, gameManager.CurrentState);
    }

    [Test]
    public void TrySetState_TriggersGameEventsOnVictory()
    {
        bool eventFired = false;
        GameEvents.OnLevelCleared += () => eventFired = true;

        gameManager.TrySetState(GameState.Victory);

        Assert.IsTrue(eventFired);
    }
}