using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using UnityEngine;
// Our own RemoteConfigService (global namespace) hides Unity's: this alias names Unity's one
using UgsRemoteConfig = Unity.Services.RemoteConfig.RemoteConfigService;

/// <summary>
/// IRemoteConfigBackend on top of Unity Remote Config. Needs the services started and the player signed in
/// (CloudSession does it in the Boot): the request goes with the player's token, so the server answers for the
/// environment of the sign-in ("development" in the Editor and development builds, "production" in release).
///
/// Remote Config doesn't take the environment name, only its ID. Without an ID it uses the one of the sign-in;
/// with one (BootManager inspector) it asks that environment for sure. The log shows the environment that answered.
/// </summary>
public class UgsRemoteConfigBackend : IRemoteConfigBackend
{
    // Attributes for the Game Overrides (segmentation). Empty for now: the segments come in the next phase.
    private struct UserAttributes { }
    private struct AppAttributes { }

    private readonly string _environmentId;

    /// <param name="environmentId">Remote Config environment ID (a UUID). Null or empty: the environment of the sign-in.</param>
    public UgsRemoteConfigBackend(string environmentId = null)
    {
        _environmentId = environmentId;
    }

    public async Task<RemoteValues> FetchAsync()
    {
        if (!string.IsNullOrWhiteSpace(_environmentId))
            UgsRemoteConfig.Instance.SetEnvironmentID(_environmentId.Trim());

        RuntimeConfig config = await UgsRemoteConfig.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());

        // Cached / Default mean the server didn't answer: the device copy is RemoteConfigService's job
        if (config == null || config.origin != ConfigOrigin.Remote)
            throw new InvalidOperationException($"No answer from Remote Config (origin: {(config != null ? config.origin.ToString() : "none")}).");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string key in config.GetKeys())
        {
            string value = Read(config, key);
            if (value != null) values[key] = value;
        }

        Debug.Log($"[RemoteConfig] Downloaded {values.Count} keys (environment: {config.environmentId}, assignment: {config.assignmentId}).");
        return new RemoteValues(values);
    }

    // Numbers and booleans come as text ("4", "True"); a JSON value as its JSON text
    private static string Read(RuntimeConfig config, string key)
    {
        try
        {
            return config.GetString(key, null);
        }
        catch (Exception)
        {
            return config.GetJson(key, null);
        }
    }
}
