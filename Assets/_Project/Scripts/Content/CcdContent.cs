/// <summary>
/// Where the remote Addressables content lives: Unity Cloud Content Delivery (CCD), one bucket per UGS environment.
/// The Addressables profile's Remote.LoadPath is "{CcdContent.RemoteLoadPath}": the curly braces make
/// Addressables read this property at runtime, so the same build picks the bucket of its environment (development
/// in the Editor and development builds, production in release) and no profile has to be switched before a build.
///
/// IDs are public (they are in every URL the game requests); the CCD API keys used to upload are not, and never
/// go in the repo.
/// </summary>
public static class CcdContent
{
    /// <summary>Unity Cloud project (the same one as Authentication, Cloud Save and Remote Config).</summary>
    public const string ProjectId = "822f0788-c148-4aa3-bff6-9ec6384b6a0d";

    /// <summary>Bucket of the development environment (Dashboard > Cloud Content Delivery > Buckets). Empty: not created yet.</summary>
    public const string DevelopmentBucketId = "";

    /// <summary>Bucket of the production environment. Empty: not created yet.</summary>
    public const string ProductionBucketId = "";

    /// <summary>The badge the game follows. Publishing content = a new release + moving this badge to it.</summary>
    public const string Badge = "latest";

    /// <summary>
    /// Base URL of the remote content of this build's environment. Without a bucket ID the URL can't be answered:
    /// the content check fails like it does without network, and the game plays with the local levels.
    /// </summary>
    public static string RemoteLoadPath => LoadPathFor(BackendEnvironment.Current);

    public static string LoadPathFor(string environment) =>
        BuildUrl(environment, environment == BackendEnvironment.Production ? ProductionBucketId : DevelopmentBucketId);

    public static string BuildUrl(string environment, string bucketId)
    {
        string bucket = string.IsNullOrWhiteSpace(bucketId) ? "bucket-not-set" : bucketId.Trim();
        return $"https://{ProjectId}.client-api.unity3dusercontent.com/client_api/v1/environments/{environment}" +
               $"/buckets/{bucket}/release_by_badge/{Badge}/entry_by_path/content/?path=";
    }

    public static bool HasBucket(string environment) =>
        !string.IsNullOrWhiteSpace(environment == BackendEnvironment.Production ? ProductionBucketId : DevelopmentBucketId);
}
