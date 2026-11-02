using NUnit.Framework;

public class BackendEnvironmentTests
{
    [Test]
    public void Editor_UsesDevelopment()
    {
        Assert.AreEqual(BackendEnvironment.Development, BackendEnvironment.Resolve(isEditor: true, isDevelopmentBuild: true));
    }

    [Test]
    public void DevelopmentBuild_UsesDevelopment()
    {
        Assert.AreEqual(BackendEnvironment.Development, BackendEnvironment.Resolve(isEditor: false, isDevelopmentBuild: true));
    }

    [Test]
    public void ReleaseBuild_UsesProduction()
    {
        Assert.AreEqual(BackendEnvironment.Production, BackendEnvironment.Resolve(isEditor: false, isDevelopmentBuild: false));
    }

    [Test]
    public void EnvironmentNames_MatchTheDashboard()
    {
        Assert.AreEqual("development", BackendEnvironment.Development);
        Assert.AreEqual("production", BackendEnvironment.Production);
    }
}
