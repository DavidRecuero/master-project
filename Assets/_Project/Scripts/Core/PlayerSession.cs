using UnityEngine;

/// <summary>
/// Session-only identity shown in the UI (name + avatar). Not persisted:
/// it changes with the signed-in account, unlike UserProfile.UserId.
/// </summary>
public static class PlayerSession
{
    public static string DisplayName { get; private set; } = "";
    public static string AvatarUrl { get; private set; } = "";
    public static Texture2D Avatar { get; set; }

    public static void Set(string displayName, string avatarUrl)
    {
        DisplayName = displayName ?? "";
        AvatarUrl = avatarUrl ?? "";
        Avatar = null;
    }

    public static void Reset() => Set("", "");
}