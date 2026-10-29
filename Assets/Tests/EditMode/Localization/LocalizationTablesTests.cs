using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Localization QA on the real tables, the checks a loc QA team runs before every release.
/// They read every language set up in the project, so a new language is checked with no change here.
///  - Every text exists in every language (no missing translation shows up as the key on screen).
///  - Every translation keeps the same {placeholders} as English (a missing {level} would hide the number).
///  - Every text with {placeholders} is a Smart String (otherwise "{level}" is shown as it is).
///  - Only Latin-1 characters (the static fonts don't hold anything else).
/// </summary>
public class LocalizationTablesTests
{
    private const string UiTable = "UI";
    private const string SourceLanguage = "en";

    private static readonly Regex Placeholder = new Regex(@"\{[^{}]+\}");

    private static StringTableCollection Collection()
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UiTable);
        Assert.IsNotNull(collection, $"String Table Collection '{UiTable}' not found");
        return collection;
    }

    private static IEnumerable<(string key, string language, StringTableEntry entry)> AllEntries()
    {
        StringTableCollection collection = Collection();

        foreach (SharedTableData.SharedTableEntry shared in collection.SharedData.Entries)
        {
            foreach (StringTable table in collection.StringTables)
                yield return (shared.Key, table.LocaleIdentifier.Code, table.GetEntry(shared.Id));
        }
    }

    [Test]
    public void EveryLanguageOfTheProject_HasATable()
    {
        var tableLanguages = Collection().StringTables.Select(t => t.LocaleIdentifier.Code).ToList();

        foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            CollectionAssert.Contains(tableLanguages, locale.Identifier.Code, $"No UI table for {locale.Identifier.Code}");
    }

    [Test]
    public void EveryText_IsTranslatedInEveryLanguage()
    {
        var missing = AllEntries()
            .Where(e => e.entry == null || string.IsNullOrWhiteSpace(e.entry.Value))
            .Select(e => $"{e.key} [{e.language}]")
            .ToList();

        Assert.IsEmpty(missing, "Missing translations:\n" + string.Join("\n", missing));
    }

    [Test]
    public void EveryTranslation_KeepsThePlaceholdersOfEnglish()
    {
        var source = AllEntries()
            .Where(e => e.language == SourceLanguage && e.entry != null)
            .ToDictionary(e => e.key, e => Placeholders(e.entry.Value));

        var wrong = AllEntries()
            .Where(e => e.entry != null && source.ContainsKey(e.key))
            .Where(e => !source[e.key].SetEquals(Placeholders(e.entry.Value)))
            .Select(e => $"{e.key} [{e.language}]: '{e.entry.Value}'")
            .ToList();

        Assert.IsEmpty(wrong, "Placeholders different from English:\n" + string.Join("\n", wrong));
    }

    [Test]
    public void TextsWithPlaceholders_AreSmartStrings()
    {
        var notSmart = AllEntries()
            .Where(e => e.entry != null && Placeholder.IsMatch(e.entry.Value) && !e.entry.IsSmart)
            .Select(e => $"{e.key} [{e.language}]")
            .ToList();

        Assert.IsEmpty(notSmart, "Tick 'Smart' on these entries:\n" + string.Join("\n", notSmart));
    }

    [Test]
    public void Texts_UseOnlyLatin1()
    {
        var wrong = new List<string>();
        foreach (var e in AllEntries())
        {
            if (e.entry == null || e.entry.Value == null) continue;
            foreach (char c in e.entry.Value)
            {
                if (c > 0xFF) wrong.Add($"{e.key} [{e.language}]: '{c}' (U+{(int)c:X4})");
            }
        }

        Assert.IsEmpty(wrong, "Characters the static fonts can't draw (a new language needs its own font):\n" + string.Join("\n", wrong));
    }

    private static HashSet<string> Placeholders(string text)
    {
        var set = new HashSet<string>();
        if (string.IsNullOrEmpty(text)) return set;
        foreach (Match m in Placeholder.Matches(text)) set.Add(m.Value);
        return set;
    }
}
