using Scada.Core;
using Scada.Runtime;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class LiveRuntimeContractTests
{
    [Fact]
    public void SnapshotPreservesBadNullValueAndSourceStatus()
    {
        var timestamp = Utc(12, 0);
        var status = new RuntimeSourceStatus(
            RuntimeSourceState.Disconnected,
            "memory",
            timestamp,
            null,
            [RuntimeDiagnostics.Create(RuntimeDiagnostics.SourceDisconnected, "数据源已断开。")]);
        var value = new RuntimeSnapshotValue(
            "Tank.Level",
            VariableDataType.Float64,
            null,
            VariableQuality.Bad,
            timestamp,
            0);

        var snapshot = new RuntimeSnapshot(7, "memory", timestamp, [value], status);

        Assert.Equal(7, snapshot.Sequence);
        Assert.Null(snapshot.Values.Single().Value);
        Assert.Equal(VariableQuality.Bad, snapshot.Values.Single().Quality);
        Assert.Equal(RuntimeSourceState.Disconnected, snapshot.SourceStatus.State);
        Assert.Equal(TimeSpan.Zero, snapshot.ServerTimestamp.Offset);
    }

    [Fact]
    public async Task ProviderLifecycleContractCanStartReadAndStop()
    {
        var provider = new TestProvider();
        await provider.StartAsync();

        var value = await provider.ReadAsync("Tank.Level", Utc(12, 1));

        Assert.Equal(RuntimeSourceState.Connected, provider.Status.State);
        Assert.Equal("Tank.Level", value.Key);
        await provider.StopAsync();
        Assert.Equal(RuntimeSourceState.Stopped, provider.Status.State);
        await provider.DisposeAsync();
    }

    private static DateTimeOffset Utc(int hour, int minute) =>
        new(2026, 8, 23, hour, minute, 0, TimeSpan.Zero);

    private sealed class TestProvider : IRuntimeDataProvider
    {
        public RuntimeSourceStatus Status { get; private set; } =
            new(RuntimeSourceState.Stopped, "test", DateTimeOffset.UtcNow, null, []);

        public event EventHandler<RuntimeVariableUpdate>? Updated;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Status = Status with { State = RuntimeSourceState.Connected, ChangedAt = DateTimeOffset.UtcNow };
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Status = Status with { State = RuntimeSourceState.Stopped, ChangedAt = DateTimeOffset.UtcNow };
            return Task.CompletedTask;
        }

        public ValueTask<RuntimeVariableValue> ReadAsync(
            string key,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RuntimeVariableValue(
                key,
                42.5d,
                VariableDataType.Float64,
                VariableQuality.Good,
                now));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Publish(RuntimeVariableValue value) =>
            Updated?.Invoke(this, new RuntimeVariableUpdate(value, "test", DateTimeOffset.UtcNow));
    }
}
