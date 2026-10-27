using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class CreditsPopupTests
{
    private const string CreditsFilePath = "_Project/Data/Credits.txt";

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
        PopupTestUtils.SetField(_prefab, "creditsFile", file);

        CreditsPopup credits = _service.Open(_prefab);

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

    // ---------- The real credits file ----------

    private static string ReadCreditsFile()
    {
        string path = Path.Combine(Application.dataPath, CreditsFilePath);
        Assert.IsTrue(File.Exists(path), $"Credits file missing: Assets/{CreditsFilePath}");
        return File.ReadAllText(path);
    }

    [Test]
    public void CreditsFile_AttributesTheCcByMusic()
    {
        // CC-BY requires the author and the license to be shown
        string text = ReadCreditsFile();

        StringAssert.Contains("Matthew Pablo", text);
        StringAssert.Contains("CC-BY 3.0", text);
    }

    [Test]
    public void CreditsFile_UsesOnlyLatin1()
    {
        // The static fonts only hold Latin-1: anything else would show as a missing glyph
        foreach (char c in ReadCreditsFile())
            Assert.LessOrEqual((int)c, 0xFF, $"'{c}' is not Latin-1");
    }
}
