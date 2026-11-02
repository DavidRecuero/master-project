using System;

/// <summary>
/// An identity provider that can hand out a one-time code for a game server.
/// Unity Authentication exchanges that code to sign the same player in on the backend.
/// Only Google Play Games implements it; the guest provider doesn't.
/// </summary>
public interface IServerAuthCodeProvider
{
    /// <summary>Calls back with the code, or with null/empty when it can't get one (not signed in, error).</summary>
    void RequestServerAuthCode(Action<string> onCode);
}
