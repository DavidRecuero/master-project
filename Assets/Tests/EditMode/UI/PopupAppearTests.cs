using NUnit.Framework;

public class PopupAppearTests
{
    [Test]
    public void Ease_StartsAtZero_OvershootsAndSettlesOnOne()
    {
        Assert.AreEqual(0f, PopupAppear.EaseOutBack(0f), 1e-4f);
        Assert.Greater(PopupAppear.EaseOutBack(0.7f), 1f, "Grows a bit past its size");
        Assert.AreEqual(1f, PopupAppear.EaseOutBack(1f), 1e-4f);
    }
}
