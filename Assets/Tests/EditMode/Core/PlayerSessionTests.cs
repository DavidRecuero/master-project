using NUnit.Framework;
using UnityEngine;

public class PlayerSessionTests
{
    [SetUp]
    public void SetUp() => PlayerSession.Reset();

    [TearDown]
    public void TearDown() => PlayerSession.Reset();

    [Test]
    public void Set_StoresNameAndAvatarUrl()
    {
        PlayerSession.Set("Ana", "https://example.com/a.png");

        Assert.AreEqual("Ana", PlayerSession.DisplayName);
        Assert.AreEqual("https://example.com/a.png", PlayerSession.AvatarUrl);
    }

    [Test]
    public void Set_ClearsThePreviousAvatarTexture()
    {
        var texture = new Texture2D(2, 2);
        PlayerSession.Avatar = texture;

        PlayerSession.Set("Ben", "https://example.com/b.png");

        Assert.IsNull(PlayerSession.Avatar);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void Set_TreatsNullsAsEmpty()
    {
        PlayerSession.Set(null, null);

        Assert.AreEqual("", PlayerSession.DisplayName);
        Assert.AreEqual("", PlayerSession.AvatarUrl);
    }

    [Test]
    public void Reset_ClearsEverything()
    {
        PlayerSession.Set("Ana", "https://example.com/a.png");

        PlayerSession.Reset();

        Assert.AreEqual("", PlayerSession.DisplayName);
        Assert.AreEqual("", PlayerSession.AvatarUrl);
        Assert.IsNull(PlayerSession.Avatar);
    }
}