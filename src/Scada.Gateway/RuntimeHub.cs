using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Scada.Gateway;

[Authorize(Policy = "Runtime.View")]
public sealed class RuntimeHub : Hub
{
    private readonly RuntimeSubscriptionRegistry _registry;
    private readonly RuntimeDataCoordinator _coordinator;
    private readonly Scada.Runtime.IRuntimeProjectSource _projectSource;

    public RuntimeHub(
        RuntimeSubscriptionRegistry registry,
        RuntimeDataCoordinator coordinator,
        Scada.Runtime.IRuntimeProjectSource projectSource)
    {
        _registry = registry;
        _coordinator = coordinator;
        _projectSource = projectSource;
    }

    public async Task Subscribe(string screen, IReadOnlyList<string> variables)
    {
        var project = await _projectSource.LoadAsync(Context.ConnectionAborted).ConfigureAwait(false);
        var screenExists = project.Status == Scada.Core.ProjectStatus.Published
            && project.Screens.Any(item => string.Equals(item.Name, screen, StringComparison.Ordinal));
        var allowedVariables = project.Variables
            .Select(variable => variable.Key)
            .ToHashSet(StringComparer.Ordinal);
        if (!screenExists)
        {
            await Clients.Caller.SendAsync("diagnostic", new
            {
                code = "SUBSCRIPTION_REJECTED",
                message = "订阅画面不存在或项目尚未发布。"
            }, Context.ConnectionAborted);
            return;
        }

        var result = _registry.Subscribe(
            Context.ConnectionId,
            Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "unknown",
            screen,
            variables,
            allowedVariables);
        if (!result.Accepted)
        {
            await Clients.Caller.SendAsync("diagnostic", new { code = result.Code, message = result.Message });
            return;
        }

        await Clients.Caller.SendAsync("snapshot", _coordinator.GetFullSnapshot(result.Subscription!.Variables));
    }

    public Task Unsubscribe()
    {
        _registry.Remove(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public Task RequestFullSnapshot()
    {
        var subscription = _registry.Get(Context.ConnectionId);
        return subscription is null
            ? Clients.Caller.SendAsync("diagnostic", new { code = "SUBSCRIPTION_REQUIRED", message = "请先订阅画面变量。" })
            : Clients.Caller.SendAsync("snapshot", _coordinator.GetFullSnapshot(subscription.Variables));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
