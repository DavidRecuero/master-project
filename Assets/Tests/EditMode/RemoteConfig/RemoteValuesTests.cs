using NUnit.Framework;

/// <summary>Reading the remote settings: typed getters, and the local value whenever a key is missing or unreadable.</summary>
public class RemoteValuesTests
{
    [Test]
    public void Empty_ReturnsEveryFallback()
    {
        RemoteValues values = RemoteValues.Empty;

        Assert.AreEqual(0, values.Count);
        Assert.AreEqual(7, values.GetInt("lives_max", 7));
        Assert.AreEqual(1.5f, values.GetFloat("delay", 1.5f));
        Assert.IsTrue(values.GetBool("analytics_enabled", true));
        Assert.AreEqual("x", values.GetString("name", "x"));
    }

    [Test]
    public void TypedValues_AreRead()
    {
        RemoteValues values = RemoteTestValues.Of("int", "12", "float", "0.25", "bool", "false", "text", "hello");

        Assert.AreEqual(12, values.GetInt("int", 0));
        Assert.AreEqual(0.25f, values.GetFloat("float", 0f));
        Assert.IsFalse(values.GetBool("bool", true));
        Assert.AreEqual("hello", values.GetString("text", ""));
    }

    [TestCase("True", true)]
    [TestCase("FALSE", false)]
    [TestCase("1", true)]
    [TestCase("0", false)]
    public void Bool_AcceptsWhatTheSdkSends(string raw, bool expected)
    {
        Assert.AreEqual(expected, RemoteTestValues.Of("b", raw).GetBool("b", !expected));
    }

    [TestCase("abc")]
    [TestCase("")]
    [TestCase("4.5")]
    public void UnreadableInt_ReturnsFallback(string raw)
    {
        Assert.AreEqual(9, RemoteTestValues.Of("n", raw).GetInt("n", 9));
    }

    [Test]
    public void Float_IgnoresTheDeviceCulture()
    {
        // The server sends "0.5"; a Spanish device would read "0,5" with the current culture
        Assert.AreEqual(0.5f, RemoteTestValues.Of("f", "0.5").GetFloat("f", 0f));
    }

    [Test]
    public void ToDictionary_IsACopy()
    {
        RemoteValues values = RemoteTestValues.Of("a", "1");
        values.ToDictionary()["a"] = "2";

        Assert.AreEqual(1, values.GetInt("a", 0));
    }
}
