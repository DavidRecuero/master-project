using System;

/// <summary>
/// The groups the server put the player in, set by the Game Overrides on the dashboard. Three Remote Config keys,
/// one per GameAnalytics custom dimension, so each one answers one question and they never fight:
///   - "segment"    (dimension 01): the player's current state. Several overrides set it; the priority picks one.
///   - "experiment" (dimension 02): the A/B variant.
///   - "lifecycle"  (dimension 03): how long ago the player started (early / mature).
///
/// GameAnalytics only accepts the values declared in Resources/GameAnalytics/Settings (Custom Dimensions 01-03),
/// so they are a fixed list here too: a value the game doesn't know is reported as "other" or "none".
/// A new value on the dashboard needs its name added here and in those settings (a test checks they match).
/// </summary>
public static class PlayerSegments
{
    // Segments (custom dimension 01), from the highest override priority to the lowest
    public const string Default = "default";
    public const string Stuck = "stuck";
    public const string Returning = "returning";
    public const string New = "new";
    public const string Loop = "loop";
    public const string Veteran = "veteran";
    public const string Active = "active";
    public const string Other = "other";

    // A/B variants (custom dimension 02). One experiment at a time: its name is in the dashboard and in the docs
    public const string NoExperiment = "none";
    public const string VariantA = "a";
    public const string VariantB = "b";
    public const string VariantC = "c";

    // Lifecycle (custom dimension 03)
    public const string NoLifecycle = "none";
    public const string Early = "early";
    public const string Mature = "mature";

    public static readonly string[] Segments = { Default, Stuck, Returning, New, Loop, Veteran, Active, Other };
    public static readonly string[] Variants = { NoExperiment, VariantA, VariantB, VariantC };
    public static readonly string[] Lifecycles = { NoLifecycle, Early, Mature };

    public static string Segment(RemoteValues remote) =>
        Known(Read(remote, RemoteKeys.Segment), Segments, Default, Other);

    public static string Experiment(RemoteValues remote) =>
        Known(Read(remote, RemoteKeys.Experiment), Variants, NoExperiment, NoExperiment);

    public static string Lifecycle(RemoteValues remote) =>
        Known(Read(remote, RemoteKeys.Lifecycle), Lifecycles, NoLifecycle, NoLifecycle);

    private static string Read(RemoteValues remote, string key) => (remote ?? RemoteValues.Empty).GetString(key, null);

    private static string Known(string raw, string[] known, string empty, string unknown)
    {
        string value = raw?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value)) return empty;
        return Array.IndexOf(known, value) >= 0 ? value : unknown;
    }
}
