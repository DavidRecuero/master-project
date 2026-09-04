using System.Collections;
using NUnit.Framework;

public class GameEventsTests
{
    [Test]
    public void TriggerLevelCleared_InvokesOnLevelClearedEvent()
    {
        bool eventFired = false;
        GameEvents.OnLevelCleared += () => eventFired = true;

        GameEvents.TriggerLevelCleared();

        Assert.IsTrue(eventFired);
    }

    [Test]
    public void TriggerLevelFailed_InvokesOnLevelFailedEvent()
    {
        bool eventFired = false;
        GameEvents.OnLevelFailed += () => eventFired = true;

        GameEvents.TriggerLevelFailed();

        Assert.IsTrue(eventFired);
    }
}