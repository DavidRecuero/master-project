using NUnit.Framework;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class LocalizedStringExtensionsTests
{
    [Test]
    public void SetVariable_CreatesItTheFirstTime()
    {
        var text = new LocalizedString();

        text.SetVariable("level", 7);

        Assert.AreEqual(7, text.GetVariableValue("level"));
    }

    [Test]
    public void SetVariable_UpdatesTheSameVariable()
    {
        var text = new LocalizedString();
        text.SetVariable("id", "abc");
        text.TryGetValue("id", out IVariable first);

        text.SetVariable("id", "xyz");
        text.TryGetValue("id", out IVariable second);

        Assert.AreSame(first, second, "Same variable, so the text refreshes on its own");
        Assert.AreEqual("xyz", text.GetVariableValue("id"));
    }

    [Test]
    public void SetVariable_ReplacesAVariableOfAnotherType()
    {
        var text = new LocalizedString();
        text.SetVariable("level", "one");

        text.SetVariable("level", 1);

        Assert.AreEqual(1, text.GetVariableValue("level"));
    }

    [Test]
    public void MissingVariable_IsNull()
    {
        Assert.IsNull(new LocalizedString().GetVariableValue("nope"));
    }
}
