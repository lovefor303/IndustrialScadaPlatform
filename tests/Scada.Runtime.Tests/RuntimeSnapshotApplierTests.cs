using Scada.Core;
using Scada.Runtime;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeSnapshotApplierTests
{
    [Fact]
    public void AppliesOnlyKnownKeysAndIgnoresOlderSequence()
    {
        var sink = new RecordingSink();
        var applier = new RuntimeSnapshotApplier(sink, ["Tank.Level"]);
        var status = new RuntimeSourceStatus(
            RuntimeSourceState.Connected,
            "memory",
            SampleTime,
            SampleTime,
            []);

        applier.Apply(Snapshot(2, status, new RuntimeSnapshotValue(
            "Tank.Level", VariableDataType.Float64, 42.5d, VariableQuality.Good, SampleTime, 0)));
        applier.Apply(Snapshot(1, status, new RuntimeSnapshotValue(
            "Tank.Level", VariableDataType.Float64, 12.5d, VariableQuality.Good, SampleTime, 0),
            new RuntimeSnapshotValue("Unknown", VariableDataType.Float64, 99d, VariableQuality.Good, SampleTime, 0)));

        var applied = Assert.Single(sink.Values);
        Assert.Equal(42.5d, applied.Value);
        Assert.Equal(VariableQuality.Good, applied.Quality);
        Assert.Equal(2, applier.LastSequence);
    }

    [Fact]
    public void AppliesBadQualityWithoutFabricatingValue()
    {
        var sink = new RecordingSink();
        var applier = new RuntimeSnapshotApplier(sink, ["Tank.Level"]);
        var status = new RuntimeSourceStatus(
            RuntimeSourceState.Disconnected,
            "memory",
            SampleTime,
            null,
            []);

        applier.Apply(Snapshot(1, status, new RuntimeSnapshotValue(
            "Tank.Level", VariableDataType.Float64, null, VariableQuality.Bad, SampleTime, 5000)));

        var applied = Assert.Single(sink.Values);
        Assert.Null(applied.Value);
        Assert.Equal(VariableQuality.Bad, applied.Quality);
    }

    private static readonly DateTimeOffset SampleTime =
        new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    private static RuntimeSnapshot Snapshot(
        long sequence,
        RuntimeSourceStatus status,
        params RuntimeSnapshotValue[] values) =>
        new(sequence, "memory", SampleTime, values, status);

    private sealed class RecordingSink : IRuntimeVariableSink
    {
        public List<RuntimeSnapshotValue> Values { get; } = [];

        public void Apply(RuntimeSnapshot snapshot)
        {
            Values.AddRange(snapshot.Values);
        }
    }
}
