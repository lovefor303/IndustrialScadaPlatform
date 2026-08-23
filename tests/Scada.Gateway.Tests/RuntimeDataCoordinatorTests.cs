using Scada.Core;
using Scada.Gateway;
using Scada.Runtime;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class RuntimeDataCoordinatorTests
{
    private static readonly DateTimeOffset SampleTime =
        new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartsAndStopsOneProviderAndExposesSnapshots()
    {
        var provider = new FakeProvider();
        var cache = new RuntimeSnapshotCache("fake");
        await using var coordinator = new RuntimeDataCoordinator(provider, cache);

        await coordinator.StartAsync();
        Assert.Equal(1, provider.StartCount);
        Assert.Equal(RuntimeSourceState.Connected, coordinator.Status.State);

        provider.Publish(new RuntimeVariableValue(
            "Tank.Level", 8d, VariableDataType.Float64, VariableQuality.Good, SampleTime));
        var snapshot = coordinator.GetFullSnapshot(["Tank.Level"], SampleTime.AddSeconds(1));
        Assert.Equal(8d, snapshot.Values.Single().Value);

        await coordinator.StopAsync();
        Assert.Equal(1, provider.StopCount);
        Assert.Equal(RuntimeSourceState.Stopped, coordinator.Status.State);
    }

    [Fact]
    public async Task ProviderFailureBecomesDisconnectedWithoutEscapingCoordinator()
    {
        var provider = new FakeProvider { ThrowOnStart = true };
        var cache = new RuntimeSnapshotCache("fake");
        await using var coordinator = new RuntimeDataCoordinator(provider, cache);

        await coordinator.StartAsync();

        Assert.Equal(RuntimeSourceState.Disconnected, coordinator.Status.State);
        Assert.Contains(coordinator.Status.Diagnostics, diagnostic =>
            diagnostic.Code == RuntimeDiagnostics.SourceDisconnected);
    }

    private sealed class FakeProvider : IRuntimeDataProvider
    {
        private RuntimeSourceStatus _status = new(
            RuntimeSourceState.Stopped,
            "fake",
            SampleTime,
            null,
            []);

        public bool ThrowOnStart { get; init; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public RuntimeSourceStatus Status => _status;
        public event EventHandler<RuntimeVariableUpdate>? Updated;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            if (ThrowOnStart)
            {
                throw new InvalidOperationException("simulated provider failure");
            }

            _status = _status with { State = RuntimeSourceState.Connected };
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;
            _status = _status with { State = RuntimeSourceState.Stopped };
            return Task.CompletedTask;
        }

        public ValueTask<RuntimeVariableValue> ReadAsync(
            string key,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RuntimeVariableValue(
                key, null, VariableDataType.String, VariableQuality.Bad, now));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Publish(RuntimeVariableValue value) =>
            Updated?.Invoke(this, new RuntimeVariableUpdate(value, "fake", value.Timestamp));
    }
}
