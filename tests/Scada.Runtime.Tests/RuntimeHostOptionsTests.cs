using Scada.Runtime.Preview;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeHostOptionsTests
{
    [Fact]
    public void JsonSourceDefaultsToLoopbackAndEphemeralPort()
    {
        var options = RuntimeHostOptions.Parse(["--project", "project.json"]);

        Assert.Equal("project.json", options.ProjectPath);
        Assert.Equal("http://127.0.0.1:0", options.Urls);
        Assert.False(options.AllowNonLoopback);
    }

    [Fact]
    public void RevisionSourceRequiresAllRevisionArguments()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RuntimeHostOptions.Parse(["--db", "runtime.db", "--project-id", Guid.NewGuid().ToString()]));

        Assert.Contains("同时提供", exception.Message);
    }

    [Fact]
    public void NonLoopbackUrlRequiresExplicitOptIn()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RuntimeHostOptions.Parse(["--project", "project.json", "--urls", "http://0.0.0.0:5000"]));

        Assert.Contains("回环地址", exception.Message);
    }
}
