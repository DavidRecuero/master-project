using NUnit.Framework;
using UnityEngine;

/// <summary>Idle animation curves of the main menu machine.</summary>
public class MenuMachineTests
{
    [Test]
    public void Drops_RestMostOfTheLoop_AndHopNearTheEnd()
    {
        Assert.AreEqual(new Vector3(1f, 1f, 0f), MenuMachineFx.HopPose(0.5f));
        Assert.AreEqual(1f, MenuMachineFx.HopPose(0.78f).z, 1e-4f, "Highest point");
        Assert.Less(MenuMachineFx.HopPose(0.86f).y, 1f, "Squashed on landing");
    }

    [Test]
    public void Panel_SquishesAndComesBackToItsSize()
    {
        Assert.AreEqual(Vector2.one, MenuMachineFx.SquishScale(0f));
        Assert.Less(MenuMachineFx.SquishScale(0.45f).y, 1f);
        Assert.Greater(MenuMachineFx.SquishScale(0.6f).y, 1f);
        Assert.AreEqual(1f, MenuMachineFx.SquishScale(0.999f).x, 1e-3f);
    }
}
