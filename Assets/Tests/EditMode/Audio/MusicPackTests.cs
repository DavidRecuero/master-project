using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// The music asset pack (Play Asset Delivery): every track the scenes ask for is in the pack, the pack is
/// fast-follow with the right name, and no song is left in the build as an imported AudioClip.
/// </summary>
public class MusicPackTests
{
    private const string PackFolder = "Assets/_Project/Audio/MusicPack.androidpack";
    private static readonly string[] Scenes = { "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/Level.unity" };

    [Test]
    public void FilePaths_AreBuiltFromTheTrackName()
    {
        Assert.AreEqual("pleasant_creek.ogg", MusicPack.FileName("pleasant_creek"));
        Assert.AreEqual(Path.Combine("folder", "riverside_ride.ogg"), MusicPack.FilePath("folder", "riverside_ride"));
        StringAssert.StartsWith("file://", MusicPack.FileUri("Assets/a b.ogg"));
        StringAssert.Contains("a%20b.ogg", MusicPack.FileUri("Assets/a b.ogg"));
    }

    [Test]
    public void EverySceneTrack_IsInThePack()
    {
        foreach (string scene in Scenes)
        {
            string track = TrackOf(scene);
            Assert.IsFalse(string.IsNullOrEmpty(track), $"{scene} has no SceneMusic track");
            FileAssert.Exists(MusicPack.FilePath(MusicPack.ProjectFolder, track), $"{scene} plays '{track}', which isn't in the music pack");
        }
    }

    [Test]
    public void Pack_IsFastFollow_WithTheNameTheCodeAsksFor()
    {
        string gradle = File.ReadAllText(Path.Combine(PackFolder, "build.gradle"));

        StringAssert.Contains($"packName = \"{MusicPack.PackName}\"", gradle);
        StringAssert.Contains("deliveryType = \"fast-follow\"", gradle);
        StringAssert.EndsWith($"{MusicPack.PackName}.androidpack", PackFolder, "The folder name must match packName");
    }

    [Test]
    public void NoSong_IsImportedIntoTheBuild()
    {
        // A song imported as AudioClip ships inside the app and the pack saves nothing
        string[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Contains("/Music/"))
            .ToArray();

        CollectionAssert.IsEmpty(clips);
    }

    // The SceneMusic "track:" value of a scene file
    private static string TrackOf(string scenePath)
    {
        string line = File.ReadAllLines(scenePath).FirstOrDefault(l => l.TrimStart().StartsWith("track: "));
        return line?.Split(new[] { "track: " }, 2, System.StringSplitOptions.None)[1].Trim();
    }
}
