using NUnit.Framework;
using TMPro;
using UnityEngine;

public class SettingsInfoTests
{
    [Test]
    public void FormatInfo_ShowsVersionAndFullId()
    {
        Assert.AreEqual("Version 1.2.0\nID: a_1234567890123456789",
            SettingsPopup.FormatInfo("1.2.0", "a_1234567890123456789"));
    }

    [Test]
    public void FormatInfo_MissingValues_ShowADash()
    {
        Assert.AreEqual("Version -\nID: -", SettingsPopup.FormatInfo(null, ""));
    }

    [Test]
    public void Initialize_FillsTheFooter()
    {
        var root = new GameObject("SettingsPopup", typeof(RectTransform));
        try
        {
            var settings = root.AddComponent<SettingsPopup>();
            var info = new GameObject("Info").AddComponent<TextMeshProUGUI>();
            info.transform.SetParent(root.transform);
            PopupTestUtils.SetField(settings, "infoText", info);

            var userData = new FakeUserDataProvider();
            userData.UserId = "guest-42";

            settings.Initialize(new FakeAudioService(), userData, "0.9.1");

            Assert.AreEqual("Version 0.9.1\nID: guest-42", info.text);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
