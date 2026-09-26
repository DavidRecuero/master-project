using NUnit.Framework;

public class GuestAuthProviderTests
{
    [Test]
    public void SignIn_AlwaysSucceeds()
    {
        var provider = new GuestAuthProvider(new FakeStorageProvider());
        AuthResult? result = null;

        provider.SignIn(r => result = r);

        Assert.AreEqual(AuthResult.Success, result);
        Assert.IsTrue(provider.IsSignedIn);
    }

    [Test]
    public void SignIn_GeneratesAndPersistsAnId_WhenNoneExists()
    {
        var storage = new FakeStorageProvider();
        var provider = new GuestAuthProvider(storage);

        provider.SignIn(_ => { });

        Assert.IsNotEmpty(provider.PlayerId);
        Assert.IsTrue(storage.HasKey("GuestPlayerId"));
        Assert.AreEqual(provider.PlayerId, storage.GetString("GuestPlayerId"));
    }

    [Test]
    public void SignIn_ReusesTheSameId_AcrossInstances()
    {
        // Simulates two separate app sessions sharing the same underlying storage
        var storage = new FakeStorageProvider();

        var firstSession = new GuestAuthProvider(storage);
        firstSession.SignIn(_ => { });
        string firstId = firstSession.PlayerId;

        var secondSession = new GuestAuthProvider(storage);
        secondSession.SignIn(_ => { });

        Assert.AreEqual(firstId, secondSession.PlayerId, "A new session should reuse the persisted guest id, not generate a new one.");
    }
}