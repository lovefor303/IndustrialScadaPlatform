using Scada.Core;
using Scada.Gateway;
using Scada.Runtime;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class RuntimeSnapshotCacheTests
{
    private static readonly DateTimeOffset SampleTime =
        new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CacheStartsAtOneAndRejectsOlderSourceTimestamps()
    {
        var cache = new RuntimeSnapshotCache(
            "memory",
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20));

        Assert.True(cache.Apply(Update("Tank.Level", 10d, SampleTime)));
        Assert.False(cache.Apply(Update("Tank.Level", 1d, SampleTime.AddMilliseconds(-1))));

        var snapshot = cache.GetFullSnapshot(["Tank.Level"], SampleTime.AddSeconds(1));

        Assert.Equal(1, snapshot.Sequence);
        Assert.Equal(10d, snapshot.Values.Single().Value);
        Assert.Equal(VariableQuality.Good, snapshot.Values.Single().Quality);
    }

    [Fact]
    public void CacheMapsFreshnessWithoutFabricatingValues()
    {
        var cache = new RuntimeSnapshotCache(
            "memory",
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20));
        cache.Apply(Update("Tank.Level", 10d, SampleTime));

        var uncertain = cache.GetFullSnapshot(["Tank.Level"], SampleTime.AddSeconds(6));
        Assert.Equal(VariableQuality.Uncertain, uncertain.Values.Single().Quality);
        Assert.Equal(10d, uncertain.Values.Single().Value);
        Assert.Equal(6000, uncertain.Values.Single().AgeMilliseconds);

        var bad = cache.GetFullSnapshot(["Tank.Level"], SampleTime.AddSeconds(21));
        Assert.Equal(VariableQuality.Bad, bad.Values.Single().Quality);
        Assert.Equal(10d, bad.Values.Single().Value);

        var unknown = cache.GetFullSnapshot(["Missing"], SampleTime.AddSeconds(1));
        Assert.Null(unknown.Values.Single().Value);
        Assert.Equal(VariableQuality.Bad, unknown.Values.Single().Quality);
    }

    [Fact]
    public void CachePublishesSourceStatusInSnapshots()
    {
        var cache = new RuntimeSnapshotCache("memory");
        var changedAt = SampleTime.AddSeconds(2);
        cache.SetStatus(new RuntimeSourceStatus(
            RuntimeSourceState.Disconnected,
            "memory",
            changedAt,
            SampleTime,
            [RuntimeDiagnostics.Create(RuntimeDiagnostics.SourceDisconnected, "数据源已断开。" )]));

        var snapshot = cache.GetFullSnapshot([], SampleTime.AddSeconds(3));

        Assert.Equal(RuntimeSourceState.Disconnected, snapshot.SourceStatus.State);
        Assert.Equal(changedAt, snapshot.SourceStatus.ChangedAt);
    }

    [Fact]
    public void IncrementalSnapshotContainsOnlyUpdatesAfterRequestedSequence()
    {
        var cache = new RuntimeSnapshotCache("memory");
        cache.Apply(Update("Tank.Level", 10d, SampleTime));
        var firstSequence = cache.GetFullSnapshot([], SampleTime).Sequence;
        cache.Apply(Update("Tank.Level", 11d, SampleTime.AddSeconds(1)));
        cache.Apply(Update("Pump.Run", true, SampleTime.AddSeconds(2)) with
        {
            Value = new RuntimeVariableValue(
                "Pump.Run", true, VariableDataType.Bool, VariableQuality.Good, SampleTime.AddSeconds(2))
        });

        var incremental = cache.GetIncrementalSnapshot(
            ["Tank.Level", "Pump.Run"], firstSequence, SampleTime.AddSeconds(3));

        Assert.Equal(2, incremental.Values.Count);
        Assert.Contains(incremental.Values, value => value.Key == "Tank.Level" && AssertValue(value.Value, 11d));
        Assert.Contains(incremental.Values, value => value.Key == "Pump.Run" && AssertValue(value.Value, true));
    }

    private static RuntimeVariableUpdate Update(string key, object value, DateTimeOffset timestamp) =>
        new(
            new RuntimeVariableValue(
                key,
                value,
                VariableDataType.Float64,
                VariableQuality.Good,
                timestamp),
            "memory",
            timestamp);

    private static bool AssertValue(object? actual, object expected) => Equals(actual, expected);
}
