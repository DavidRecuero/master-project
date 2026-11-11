using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Item animations: the curves, and that outside Play mode every movement ends at once,
/// so the logic (and the other tests) never depend on an animation.
/// </summary>
public class ItemMotionTests
{
    private GameObject _go;
    private ItemMotion _motion;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Item");
        _motion = _go.AddComponent<ItemMotion>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Easings_StartAt0_AndEndAt1()
    {
        Assert.AreEqual(0f, Easing.OutCubic(0f), 1e-5f);
        Assert.AreEqual(1f, Easing.OutCubic(1f), 1e-5f);
        Assert.AreEqual(0f, Easing.InOutCubic(0f), 1e-5f);
        Assert.AreEqual(0.5f, Easing.InOutCubic(0.5f), 1e-5f);
        Assert.AreEqual(1f, Easing.InOutCubic(1f), 1e-5f);
        Assert.AreEqual(0f, Easing.OutBack(0f), 1e-5f);
        Assert.AreEqual(1f, Easing.OutBack(1f), 1e-5f);
    }

    [Test]
    public void OutBack_GoesPastTheEnd_BeforeSettling()
    {
        Assert.Greater(Easing.OutBack(0.7f, 1.9f), 1f, "The bounce of the item reaching the platform");
        Assert.LessOrEqual(Easing.OutBack(0.7f, 0f), 1f, "Overshoot 0: no bounce");
    }

    [Test]
    public void ArcPoint_StartsAndEndsOnTheLine_AndRisesInTheMiddle()
    {
        var from = new Vector3(1f, -2f, 0f);
        var to = new Vector3(-3f, -5f, 0f);

        Assert.AreEqual(from, ItemMotion.ArcPoint(from, to, 1.25f, 0f));
        Assert.AreEqual(to, ItemMotion.ArcPoint(from, to, 1.25f, 1f));

        Vector3 middle = ItemMotion.ArcPoint(from, to, 1.25f, 0.5f);
        Assert.AreEqual((from.y + to.y) / 2f + 1.25f, middle.y, 1e-5f);
    }

    [Test]
    public void LandBounce_IsZeroAtBothEnds()
    {
        Assert.AreEqual(0f, ItemMotion.LandBounce(0f));
        Assert.AreEqual(0f, ItemMotion.LandBounce(1f));
        Assert.Greater(ItemMotion.LandBounce(0.3f), 0f);
    }

    [Test]
    public void StepDelay_MakesAWaveUpThePipe()
    {
        ItemMotionSettings s = ItemMotionSettings.Defaults;

        Assert.AreEqual(s.stepStartDelay, s.StepDelay(0), 1e-5f);
        Assert.AreEqual(s.stepStartDelay + 3 * s.stepStagger, s.StepDelay(3), 1e-5f);
        Assert.Greater(s.StepDelay(2), s.StepDelay(1), "The ones further from the exit start later");
    }

    [Test]
    public void OutsidePlayMode_SlideEndsAtOnce()
    {
        var target = new Vector3(2f, 3f, 0f);

        _motion.SlideTo(target, 0.1f, 0.2f, true, 0.1f, 0.1f);

        Assert.AreEqual(target, _go.transform.position);
        Assert.IsFalse(_motion.IsMoving);
    }

    [Test]
    public void OutsidePlayMode_FlightLandsAtOnce_AndCallsBackOnce()
    {
        int landed = 0;
        _motion.SetBaseScale(0.86f);

        _motion.FlyTo(new Vector3(-1f, -4f, 0f), 1f, 0.86f, () => landed++);
        _motion.Complete();

        Assert.AreEqual(new Vector3(-1f, -4f, 0f), _go.transform.position);
        Assert.AreEqual(1, landed);
        Assert.AreEqual(0.86f, _go.transform.localScale.x, 1e-5f, "Lands at its tray size");
    }

    [Test]
    public void InTheTray_TheSizeStaysFixed()
    {
        _motion.FlyTo(Vector3.zero, 1.006f, 0.86f, null);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ignored"));
        _motion.SetBaseScale(1.006f);
        _motion.GrowTo(0.86f, 1.006f, 0f);
        _motion.SetPulse(true, 14f, 0.02f);

        Assert.AreEqual(0.86f, _motion.BaseScale, 1e-5f);
        Assert.AreEqual(0.86f, _go.transform.localScale.x, 1e-5f);
    }

    [Test]
    public void BackToThePool_TheSizeCanChangeAgain()
    {
        _motion.FlyTo(Vector3.zero, 1.006f, 0.86f, null);
        _motion.ResetMotion();

        _motion.SetBaseScale(1.006f);

        Assert.AreEqual(1.006f, _motion.BaseScale, 1e-5f);
    }

    [Test]
    public void OutsidePlayMode_GrowthEndsAtTheRestingSize()
    {
        _motion.GrowTo(0.86f, 1.0f, 0.1f);

        Assert.IsFalse(_motion.IsGrowing);
        Assert.AreEqual(1f, _go.transform.localScale.x, 1e-5f);
    }

    [Test]
    public void Retarget_WhenNotMoving_JustPlacesIt()
    {
        _motion.Retarget(new Vector3(5f, 0f, 0f));

        Assert.AreEqual(new Vector3(5f, 0f, 0f), _go.transform.position);
    }

    // ---------- Route along the pipe (fast taps follow the tube, corners included) ----------

    private static readonly Vector3[] Corner =
    {
        new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, 1f, 0f),
    };

    [Test]
    public void RoutePoint_StartsAndEndsOnTheRoute()
    {
        Assert.AreEqual(Corner[0], ItemMotion.RoutePoint(Corner, 0f));
        Assert.AreEqual(Corner[2], ItemMotion.RoutePoint(Corner, 1f));
    }

    [Test]
    public void RoutePoint_GoesThroughTheCorner_NotAcrossIt()
    {
        // Half way of an L of two cells is the corner itself (a straight line would be at 0.5, 0.5)
        Assert.AreEqual(Corner[1], ItemMotion.RoutePoint(Corner, 0.5f));

        Vector3 quarter = ItemMotion.RoutePoint(Corner, 0.25f, out Vector3 firstLeg, out int next1);
        Assert.AreEqual(new Vector3(0.5f, 0f, 0f), quarter);
        Assert.AreEqual(Vector3.right, firstLeg.normalized);
        Assert.AreEqual(1, next1);

        Vector3 threeQuarters = ItemMotion.RoutePoint(Corner, 0.75f, out Vector3 secondLeg, out int next2);
        Assert.AreEqual(new Vector3(1f, 0.5f, 0f), threeQuarters);
        Assert.AreEqual(Vector3.up, secondLeg.normalized);
        Assert.AreEqual(2, next2);
    }

    [Test]
    public void RoutePoint_IsMeasuredByDistance()
    {
        // A short leg (the rest of a cell) and a full cell: half the distance is inside the long leg
        var route = new[] { new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, 1f, 0f) };

        Assert.AreEqual(1.5f, ItemMotion.RouteLength(route), 1e-5f);
        Assert.AreEqual(new Vector3(1f, 0.25f, 0f), ItemMotion.RoutePoint(route, 0.5f));
    }

    [Test]
    public void RoutePoint_HandlesShortRoutes()
    {
        Assert.AreEqual(Vector3.zero, ItemMotion.RoutePoint(new Vector3[0], 0.5f));
        Assert.AreEqual(Vector3.one, ItemMotion.RoutePoint(new[] { Vector3.one }, 0.5f));
        Assert.AreEqual(Vector3.one, ItemMotion.RoutePoint(new[] { Vector3.one, Vector3.one }, 0.5f));
    }

    [Test]
    public void ChainSlide_OutsidePlayMode_EndsAtTheLastTarget()
    {
        _motion.SlideTo(Corner[1], 0.1f, 0.17f, true, 0.12f, 0.14f, chain: true);
        _motion.SlideTo(Corner[2], 0.1f, 0.17f, true, 0.12f, 0.14f, chain: true);

        Assert.AreEqual(Corner[2], _go.transform.position);
        Assert.IsFalse(_motion.IsMoving);
    }
}
