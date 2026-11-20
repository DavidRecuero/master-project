using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// The levels the game can play: level number → level JSON. Built from the Addressables entries with the "level"
/// label (address "level_001", "level_002"...). Pure, so the rules are tested without loading anything.
///
/// The levels are played in order from 1, so a gap ends the sequence: level_010 without level_009 hides every
/// level after the gap (Count stops at 8). LevelLoaderTests checks the shipped levels have no gaps.
/// </summary>
public sealed class LevelCatalog
{
    public const string AddressPrefix = "level_";

    public static readonly LevelCatalog Empty = new LevelCatalog(null);

    // "level_007", also as a file name or path: "Assets/.../level_007.json"
    private static readonly Regex AddressPattern = new Regex(@"(?:^|[/\\])level_(\d+)(?:\.json)?$", RegexOptions.IgnoreCase);

    private readonly Dictionary<int, string> _levels = new Dictionary<int, string>();

    /// <param name="levels">Address (or file path) → JSON. Keys that aren't "level_NNN" are ignored.</param>
    public LevelCatalog(IEnumerable<KeyValuePair<string, string>> levels)
    {
        if (levels != null)
        {
            foreach (KeyValuePair<string, string> entry in levels)
            {
                if (string.IsNullOrEmpty(entry.Value)) continue;
                if (TryParseNumber(entry.Key, out int number)) _levels[number] = entry.Value;
            }
        }

        Count = 0;
        while (_levels.ContainsKey(Count + 1)) Count++;
    }

    /// <summary>Levels playable in order: 1, 2, 3... up to the first missing number.</summary>
    public int Count { get; }

    /// <summary>Every level found, gaps included (to report them).</summary>
    public IReadOnlyList<int> Numbers => _levels.Keys.OrderBy(n => n).ToList();

    public bool TryGetJson(int number, out string json) => _levels.TryGetValue(number, out json);

    /// <summary>1 → "level_001". The Addressables address and the file name (without .json).</summary>
    public static string Address(int number) => $"{AddressPrefix}{number:D3}";

    public static bool TryParseNumber(string key, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(key)) return false;

        Match match = AddressPattern.Match(key.Trim());
        return match.Success && int.TryParse(match.Groups[1].Value, out number) && number > 0;
    }
}
