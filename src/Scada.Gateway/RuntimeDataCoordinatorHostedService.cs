using Microsoft.Extensions.Hosting;

namespace Scada.Gateway;

public sealed class RuntimeDataCoordinatorHostedService : IHostedService
{
    private readonly RuntimeDataCoordinator _coordinator;

    public RuntimeDataCoordinatorHostedService(RuntimeDataCoordinator coordinator) =>
        _coordinator = coordinator;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _coordinator.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        _coordinator.StopAsync(cancellationToken);
}
