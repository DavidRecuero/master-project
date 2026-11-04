using NUnit.Framework;
using UnityEngine;

/// <summary>The moving stripes of the exit platforms: frame maths and start/stop.</summary>
public class PlatformStripesFxTests
{
    [TestCase(0f, 0)]
    [TestCase(0.049f, 0)]
    [TestCase(0.051f, 1)]
    [TestCase(0.79f, 15)]
    [TestCase(0.801f, 0)]      // 16 frames at 20 fps: the loop lasts 0.8 s and starts again
    [TestCase(8.051f, 1)]
    public void FrameAt_LoopsOverTheFrames(float time, int expected)
    {
        Assert.AreEqual(expected, PlatformStripesFx.FrameAt(time, 20f, 16));
    }

    [Test]
    public void FrameAt_WithNoFramesOrSpeed_IsTheFirstFrame()
    {
        Assert.AreEqual(0, PlatformStripesFx.FrameAt(3f, 20f, 0));
        Assert.AreEqual(0, PlatformStripesFx.FrameAt(3f, 0f, 16));
    }

    [Test]
    public void SetPlaying_ShowsAFrame_AndStopping_LetsPipeViewSetTheSprite()
    {
        var go = new GameObject("Platform");
        try
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            PlatformStripesFx fx = go.AddComponent<PlatformStripesFx>();
            Sprite[] frames = { MakeSprite(), MakeSprite() };
            fx.Initialize(sr, frames, 20f);

            fx.SetPlaying(true);
            Assert.IsTrue(fx.IsPlaying);
            Assert.IsTrue(fx.enabled);
            CollectionAssert.Contains(frames, sr.sprite);

            fx.SetPlaying(false);
            Assert.IsFalse(fx.IsPlaying);
            Assert.IsFalse(fx.enabled, "No Update while the platform is off");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SetPlaying_WithoutFrames_NeverPlays()
    {
        var go = new GameObject("Platform");
        try
        {
            PlatformStripesFx fx = go.AddComponent<PlatformStripesFx>();
            fx.Initialize(go.GetComponent<SpriteRenderer>(), new Sprite[0], 20f);

            fx.SetPlaying(true);

            Assert.IsFalse(fx.IsPlaying);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(4, 4);
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }
}
