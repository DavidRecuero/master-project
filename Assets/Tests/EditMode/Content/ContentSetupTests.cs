using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

/// <summary>
/// Remote content (CCD): the URL of each environment and the Addressables settings it relies on. A change in the
/// Addressables windows that breaks the remote content shows up here instead of in a build already on the store.
/// </summary>
public class ContentSetupTests
{
    private const string LocalGroup = "Levels";
    private const string RemoteGroup = "Levels Remote";

    // ---------- CCD URL ----------

    [Test]
    public void Url_HasTheProjectEnvironmentBucketAndBadge()
    {
        string url = CcdContent.BuildUrl(BackendEnvironment.Development, "abc-123");

        Assert.AreEqual(
            "https://822f0788-c148-4aa3-bff6-9ec6384b6a0d.client-api.unity3dusercontent.com/client_api/v1/environments/development" +
            "/buckets/abc-123/release_by_badge/latest/entry_by_path/content/?path=", url);
    }

    [Test]
    public void Url_WithoutBucket_IsAnUnanswerableUrl_NotAnEmptyOne()
    {
        // An empty path would make Addressables look for the catalog on the device
        StringAssert.Contains("/buckets/bucket-not-set/", CcdContent.BuildUrl(BackendEnvironment.Production, " "));
    }

    [Test]
    public void EachEnvironment_HasItsOwnBucket()
    {
        StringAssert.Contains("/environments/development/", CcdContent.LoadPathFor(BackendEnvironment.Development));
        StringAssert.Contains("/environments/production/", CcdContent.LoadPathFor(BackendEnvironment.Production));
    }

    // ---------- Addressables settings ----------

    private static AddressableAssetSettings Settings
    {
        get
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "No Addressables settings in the project");
            return settings;
        }
    }

    [Test]
    public void RemoteCatalog_IsBuilt_AndUpdatedByTheBoot()
    {
        Assert.IsTrue(Settings.BuildRemoteCatalog, "Without a remote catalog new content can't be published");
        Assert.IsTrue(Settings.DisableCatalogUpdateOnStartup, "The Boot updates the catalog itself (with a time limit)");
    }

    [Test]
    public void RemoteLoadPath_IsResolvedAtRuntimeByCcdContent()
    {
        string value = Settings.profileSettings.GetValueByName(Settings.activeProfileId, "Remote.LoadPath");

        Assert.AreEqual("{CcdContent.RemoteLoadPath}", value);
    }

    [Test]
    public void LocalLevels_ShipInTheBuild_AndNeverChangeAfterRelease()
    {
        AddressableAssetGroup group = Settings.FindGroup(LocalGroup);
        Assert.IsNotNull(group, $"Group '{LocalGroup}' missing");

        Assert.AreEqual("Local.LoadPath", group.GetSchema<BundledAssetGroupSchema>().LoadPath.GetName(Settings));
        Assert.IsTrue(group.GetSchema<ContentUpdateGroupSchema>().StaticContent,
            "New levels go to the remote group; the shipped ones stay as they are");
    }

    [Test]
    public void RemoteLevels_LoadFromCcd_AndCanBeUpdated()
    {
        AddressableAssetGroup group = Settings.FindGroup(RemoteGroup);
        Assert.IsNotNull(group, $"Group '{RemoteGroup}' missing");

        var bundled = group.GetSchema<BundledAssetGroupSchema>();
        Assert.AreEqual("Remote.BuildPath", bundled.BuildPath.GetName(Settings));
        Assert.AreEqual("Remote.LoadPath", bundled.LoadPath.GetName(Settings));
        Assert.IsFalse(group.GetSchema<ContentUpdateGroupSchema>().StaticContent);
    }

    [Test]
    public void EveryRemoteLevel_HasItsAddressAndLabel()
    {
        AddressableAssetGroup group = Settings.FindGroup(RemoteGroup);
        Assert.IsNotNull(group);

        foreach (AddressableAssetEntry entry in group.entries)
        {
            Assert.IsTrue(LevelCatalog.TryParseNumber(entry.address, out int number), $"'{entry.address}' is not a level_NNN address");
            Assert.AreEqual(LevelCatalog.Address(number), entry.address, $"'{entry.address}' must be three digits");
            CollectionAssert.Contains(entry.labels, LevelLibrary.Label, $"{entry.address} has no '{LevelLibrary.Label}' label");
        }
    }
}
