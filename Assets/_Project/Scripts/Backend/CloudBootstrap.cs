using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The cloud part of the boot, in order: start the services, sign in, sync the profile.
/// Never throws: on any failure the game goes on with the data on the device (offline mode).
/// </summary>
public class CloudBootstrap
{
    private readonly IBackendAuthService _auth;
    private readonly CloudProfileSync _sync;
    private readonly string _environment;

    public CloudBootstrap(IBackendAuthService auth, CloudProfileSync sync, string environment)
    {
        _auth = auth;
        _sync = sync;
        _environment = environment;
    }

    /// <summary>
    /// True if the profile is in sync with the cloud.
    /// playGamesAuthCode: null for anonymous sign-in (guest).
    /// </summary>
    public async Task<bool> RunAsync(string playGamesAuthCode)
    {
        try
        {
            await _auth.InitializeAsync(_environment);
            await _auth.SignInAsync(playGamesAuthCode);

            string method = string.IsNullOrEmpty(playGamesAuthCode) ? "anonymous" : "Play Games";
            Debug.Log($"[Cloud] Signed in ({method}, {_environment}). PlayerId: {_auth.PlayerId}");

            ProfileSource source = await _sync.PullAsync(_auth.PlayerId);
            Debug.Log($"[Cloud] Profile synced (winner: {source}).");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] Offline mode, using the data on this device: {e.Message}");
            return false;
        }
    }
}
