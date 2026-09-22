using System;

/// <summary>
/// Local-only fallback identity: generates a GUID once and persists it via IStorageProvider,
/// so the player has a stable PlayerId even without signing into any external service.
/// This always succeeds - it's not a network call, just local generation/lookup.
/// Swap this for GooglePlayGamesAuthProvider later - same IAuthProvider contract. (wating for the google play account purchase)
/// </summary>
public class GuestAuthProvider : IAuthProvider
{
    private const string StorageKey = "GuestPlayerId";

    private readonly IStorageProvider _storage;

    public bool IsSignedIn { get; private set; }
    public string PlayerId { get; private set; }
    public string DisplayName => "Guest";

    public GuestAuthProvider(IStorageProvider storage = null)
    {
        _storage = storage ?? new PlayerPrefsStorageProvider();
    }

    public void SignIn(Action<AuthResult> onComplete)
    {
        PlayerId = _storage.HasKey(StorageKey) ? _storage.GetString(StorageKey) : CreateAndStoreNewId();
        IsSignedIn = true;

        onComplete?.Invoke(AuthResult.Success);
    }

    private string CreateAndStoreNewId()
    {
        string newId = Guid.NewGuid().ToString();
        _storage.SetString(StorageKey, newId);
        _storage.Save();
        return newId;
    }
}