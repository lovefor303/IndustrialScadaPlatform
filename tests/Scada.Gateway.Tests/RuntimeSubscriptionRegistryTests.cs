using Scada.Gateway;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class RuntimeSubscriptionRegistryTests
{
    [Fact]
    public void SubscriptionAcceptsValidatedScreenAndVariables()
    {
        var registry = new RuntimeSubscriptionRegistry(2);

        var result = registry.Subscribe(
            "connection-1",
            "user-1",
            "Main",
            ["Tank.Level", "Pump.Run"],
            new HashSet<string>(["Tank.Level", "Pump.Run"], StringComparer.Ordinal));

        Assert.True(result.Accepted);
        var subscription = Assert.Single(registry.GetAll());
        Assert.Equal("Main", subscription.Screen);
        Assert.Collection(
            subscription.Variables,
            value => Assert.Equal("Tank.Level", value),
            value => Assert.Equal("Pump.Run", value));
    }

    [Fact]
    public void SubscriptionRejectsUnknownKeysAndExcessiveCount()
    {
        var registry = new RuntimeSubscriptionRegistry(1);

        var unknown = registry.Subscribe(
            "connection-1", "user-1", "Main", ["Missing"],
            new HashSet<string>(["Tank.Level"], StringComparer.Ordinal));
        var excessive = registry.Subscribe(
            "connection-2", "user-1", "Main", ["Tank.Level", "Pump.Run"],
            new HashSet<string>(["Tank.Level", "Pump.Run"], StringComparer.Ordinal));

        Assert.False(unknown.Accepted);
        Assert.Equal("SUBSCRIPTION_REJECTED", unknown.Code);
        Assert.False(excessive.Accepted);
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void RemovingConnectionClearsItsSubscription()
    {
        var registry = new RuntimeSubscriptionRegistry(2);
        registry.Subscribe(
            "connection-1", "user-1", "Main", ["Tank.Level"],
            new HashSet<string>(["Tank.Level"], StringComparer.Ordinal));

        registry.Remove("connection-1");

        Assert.Empty(registry.GetAll());
    }
}
