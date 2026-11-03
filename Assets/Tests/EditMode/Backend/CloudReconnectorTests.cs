using NUnit.Framework;
using UnityEngine;

/// <summary>Going to the background uploads what is pending; coming back connects if offline.</summary>
public class CloudReconnectorTests
{
    private CloudReconnector _reconnector;
    private FakeCloudConnection _connection;

    [SetUp]
    public void SetUp()
    {
        _connection = new FakeCloudConnection();
        _reconnector = CloudReconnector.Watch(_connection);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_reconnector.gameObject);

    [Test]
    public void GoingToTheBackground_UploadsWhatIsPending()
    {
        _reconnector.HandlePause(true);

        Assert.AreEqual(1, _connection.FlushCalls);
        Assert.AreEqual(0, _connection.ConnectCalls);
    }

    [Test]
    public void ComingBack_TriesToConnect()
    {
        _reconnector.HandlePause(false);

        Assert.AreEqual(1, _connection.ConnectCalls);
    }

    [Test]
    public void WatchingAgain_ReusesTheSameObject_WithTheNewSession()
    {
        var newSession = new FakeCloudConnection();

        CloudReconnector again = CloudReconnector.Watch(newSession);
        again.HandlePause(false);

        Assert.AreSame(_reconnector, again);
        Assert.AreEqual(1, newSession.ConnectCalls);
        Assert.AreEqual(0, _connection.ConnectCalls);
    }
}
