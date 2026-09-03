using System.Reflection;
using Scada.Gateway;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class RuntimeHubTests
{
    [Fact]
    public void HubSurfaceIsReadOnlyAndContainsNoWriteMethod()
    {
        var methods = typeof(RuntimeHub)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        Assert.Contains("Subscribe", methods);
        Assert.Contains("Unsubscribe", methods);
        Assert.Contains("RequestFullSnapshot", methods);
        Assert.DoesNotContain(methods, name => name.Contains("Write", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, name => name.Contains("Command", StringComparison.OrdinalIgnoreCase));
    }
}
