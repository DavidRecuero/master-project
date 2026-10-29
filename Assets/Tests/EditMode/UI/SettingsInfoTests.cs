using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// Footer of the Settings popup: UI/settings.info ("Version {version}\nID: {id}") with its two variables.
/// The words come from the table (checked by LocalizationTablesTests); here, the values the popup puts in.
/// </summary>
public class SettingsInfoTests
{
    [TestCase("1.2.0", "1.2.0")]
    [TestCase("", "-")]
    [TestCase(null, "-")]
    public void SafeValue_ShowsADash_WhenMissing(string value, string expected)
    {
        Assert.AreEqual(expected, SettingsPopup.SafeValue(value));
    }

    [Test]
    public void Initialize_FillsTheFooterWithVersionAndFullId()
    {
        var root = new GameObject("SettingsPopup", typeof(RectTransform));
        try
        {
            var settings = root.AddComponent<SettingsPopup>();
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(root.transform);
            infoGo.AddComponent<TextMeshProUGUI>();
            var info = infoGo.AddComponent<LocalizeStringEvent>();
            PopupTestUtils.SetField(settings, "infoLabel", info);

            var userData = new FakeUserDataProvider();
            userData.UserId = "a_1234567890123456789";

            settings.Initialize(new FakeAudioService(), userData, "0.9.1");

            Assert.AreEqual("0.9.1", info.StringReference.GetVariableValue(SettingsPopup.VersionVariable));
            Assert.AreEqual("a_1234567890123456789", info.StringReference.GetVariableValue(SettingsPopup.IdVariable), "Full ID, for bug reports");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Initialize_WithoutId_ShowsADash()
    {
        var root = new GameObject("SettingsPopup", typeof(RectTransform));
        try
        {
            var settings = root.AddComponent<SettingsPopup>();
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(root.transform);
            infoGo.AddComponent<TextMeshProUGUI>();
            var info = infoGo.AddComponent<LocalizeStringEvent>();
            PopupTestUtils.SetField(settings, "infoLabel", info);

            settings.Initialize(new FakeAudioService(), new FakeUserDataProvider { UserId = "" }, "1.0.0");

            Assert.AreEqual("-", info.StringReference.GetVariableValue(SettingsPopup.IdVariable));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
