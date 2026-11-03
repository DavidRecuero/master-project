using NUnit.Framework;
using UnityEngine;

/// <summary>"Hold to confirm": the action only fires after holding the whole time.</summary>
public class HoldToConfirmTests
{
    private GameObject _go;
    private HoldToConfirm _hold;
    private int _completed;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Button", typeof(RectTransform));
        _hold = _go.AddComponent<HoldToConfirm>();
        _hold.HoldSeconds = 2f;
        _completed = 0;
        _hold.Completed += () => _completed++;
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_go);

    [Test]
    public void HoldingTheWholeTime_Confirms()
    {
        _hold.Press();
        _hold.Tick(1f);
        Assert.AreEqual(0, _completed);
        Assert.AreEqual(0.5f, _hold.Progress, 0.001f);

        _hold.Tick(1f);

        Assert.AreEqual(1, _completed);
    }

    [Test]
    public void ATap_DoesNotConfirm()
    {
        _hold.Press();
        _hold.Tick(0.1f);
        _hold.Release();
        _hold.Tick(5f);

        Assert.AreEqual(0, _completed);
    }

    [Test]
    public void LettingGo_EmptiesTheFill()
    {
        _hold.Press();
        _hold.Tick(1.5f);
        _hold.Release();

        _hold.Tick(1f);

        Assert.AreEqual(0f, _hold.Progress);
        Assert.IsFalse(_hold.IsHolding);
    }

    [Test]
    public void LettingGo_AndHoldingAgain_StartsAlmostFromZero()
    {
        _hold.Press();
        _hold.Tick(1.9f);
        _hold.Release();
        _hold.Tick(1f);   // drained

        _hold.Press();
        _hold.Tick(1f);

        Assert.AreEqual(0, _completed, "The time held before doesn't count");
    }

    [Test]
    public void KeepingTheFingerDown_ConfirmsOnlyOnce()
    {
        _hold.Press();
        _hold.Tick(2f);
        _hold.Tick(2f);
        _hold.Tick(2f);

        Assert.AreEqual(1, _completed);
        Assert.AreEqual(0f, _hold.Progress);
    }
}
