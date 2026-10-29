using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.TestTools;

public class CreditsPopupTests
{
    private const string CreditsFolder = "_Project/Data/Credits";

    private GameObject _serviceGo;
    private PopupService _service;
    private CreditsPopup _prefab;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();

        _prefab = PopupTestUtils.MakePrefab<CreditsPopup>("CreditsPopup");
        var body = new GameObject("Body").AddComponent<TextMeshProUGUI>();
        body.transform.SetParent(_prefab.transform);
        PopupTestUtils.SetField(_prefab, "bodyText", body);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefab.gameObject);
    }

    [Test]
    public void Open_ShowsTheTextOfTheAssignedFile()
    {
        var file = new TextAsset("<b>MUSIC</b>\nSomeone");

        CreditsPopup credits = _service.Open(_prefab);
        credits.Initialize(file);

        Assert.AreEqual("<b>MUSIC</b>\nSomeone", credits.GetComponentInChildren<TextMeshProUGUI>().text);
        Object.DestroyImmediate(file);
    }

    [Test]
    public void Open_WithoutFile_WarnsAndShowsNothing()
    {
        LogAssert.Expect(LogType.Warning, "[CreditsPopup] No credits file assigned.");

        CreditsPopup credits = _service.Open(_prefab);

        Assert.AreEqual(string.Empty, credits.GetComponentInChildren<TextMeshProUGUI>().text);
    }

    [Test]
    public void Normalize_FixesWindowsLineEndingsAndTrims()
    {
        Assert.AreEqual("A\nB\n\nC", CreditsPopup.Normalize("A\r\nB\r\n\r\nC\r\n  "));
        Assert.AreEqual(string.Empty, CreditsPopup.Normalize(null));
    }

    // ---------- The real credits files (one per language) ----------

    private static string CreditsPath(string language) =>
        Path.Combine(Application.dataPath, CreditsFolder, $"Credits_{language}.txt");

    private static IEnumerable<string> ProjectLanguages() =>
        LocalizationEditorSettings.GetLocales().Select(l => l.Identifier.Code);

    [Test]
    public void EveryLanguage_HasACreditsFile()
    {
        foreach (string language in ProjectLanguages())
            Assert.IsTrue(File.Exists(CreditsPath(language)), $"Credits file missing: Assets/{CreditsFolder}/Credits_{language}.txt");
    }

    [Test]
    public void EveryCreditsFile_AttributesTheCcByMusic()
    {
        // CC-BY requires the author and the license to be shown, whatever the language
        foreach (string language in ProjectLanguages())
        {
            string text = File.ReadAllText(CreditsPath(language));
            StringAssert.Contains("Matthew Pablo", text, language);
            StringAssert.Contains("CC-BY 3.0", text, language);
        }
    }

    [Test]
    public void EveryCreditsFile_UsesOnlyLatin1()
    {
        // The static fonts only hold Latin-1: anything else would show as a missing glyph
        foreach (string language in ProjectLanguages())
        {
            foreach (char c in File.ReadAllText(CreditsPath(language)))
                Assert.LessOrEqual((int)c, 0xFF, $"'{c}' is not Latin-1 ({language})");
        }
    }
}
