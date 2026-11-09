using NUnit.Framework;
using UnityEngine;

/// <summary>Every "play" goes through the gate: in with a life, the "out of lives" popup without one.</summary>
public class LivesGateTests
{
    private FakeLivesService _lives;
    private FakePopupService _popups;
    private NoLivesPopup _prefab;
    private int _entered;

    [SetUp]
    public void SetUp()
    {
        _lives = new FakeLivesService();
        _popups = new FakePopupService();
        _prefab = PopupTestUtils.MakePrefab<NoLivesPopup>("NoLivesPopup");
        _entered = 0;
    }

    [TearDown]
    public void TearDown()
    {
        if (_prefab != null) Object.DestroyImmediate(_prefab.gameObject);
    }

    [Test]
    public void WithALife_EntersTheLevel()
    {
        _lives.Lives = 1;

        bool entered = new LivesGate(_lives, _popups, _prefab).TryEnter(() => _entered++);

        Assert.IsTrue(entered);
        Assert.AreEqual(1, _entered);
        Assert.AreEqual(0, _popups.Count);
        Assert.AreEqual(0, _lives.UseCalls, "The level itself uses the life when it starts");
    }

    [Test]
    public void WithoutLives_OpensThePopupInstead()
    {
        _lives.Lives = 0;

        bool entered = new LivesGate(_lives, _popups, _prefab).TryEnter(() => _entered++);

        Assert.IsFalse(entered);
        Assert.AreEqual(0, _entered);
        Assert.AreSame(_prefab, _popups.Top);
    }

    [Test]
    public void BuyingInThePopup_EntersTheLevel()
    {
        _lives.Lives = 0;
        new LivesGate(_lives, _popups, _prefab).TryEnter(() => _entered++);

        _prefab.OnActionClicked();

        Assert.AreEqual(1, _lives.BuyCalls);
        Assert.AreEqual(1, _entered);
    }

    [Test]
    public void WithoutALivesSystem_LetsThePlayerIn()
    {
        bool entered = new LivesGate(null, _popups, _prefab).TryEnter(() => _entered++);

        Assert.IsTrue(entered);
        Assert.AreEqual(1, _entered);
    }

    [Test]
    public void WithoutLivesAndWithoutPrefab_DoesNothing()
    {
        _lives.Lives = 0;

        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No lives left"));
        bool entered = new LivesGate(_lives, _popups, null).TryEnter(() => _entered++);

        Assert.IsFalse(entered);
        Assert.AreEqual(0, _entered);
    }
}
