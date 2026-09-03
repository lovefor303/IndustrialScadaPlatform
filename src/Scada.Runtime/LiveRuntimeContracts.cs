using Scada.Core;

namespace Scada.Runtime;

public enum RuntimeSourceState
{
    Stopped,
    Starting,
    Connected,
    Degraded,
    Disconnected
}

public sealed record RuntimeSourceStatus(
    RuntimeSourceState State,
    string Source,
    DateTimeOffset ChangedAt,
    DateTimeOffset? LastSuccessfulRead,
    IReadOnlyList<RuntimeDiagnostic> Diagnostics);

public sealed record RuntimeVariableUpdate(
    RuntimeVariableValue Value,
    string Source,
    DateTimeOffset ReceivedAt);

public interface IRuntimeDataProvider : IAsyncDisposable
{
    RuntimeSourceStatus Status { get; }

    event EventHandler<RuntimeVariableUpdate>? Updated;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    ValueTask<RuntimeVariableValue> ReadAsync(
        string key,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public sealed record RuntimeSubscriptionRequest(
    string Screen,
    IReadOnlyList<string> Variables);

public sealed record RuntimeSnapshotValue(
    string Key,
    VariableDataType DataType,
    object? Value,
    VariableQuality Quality,
    DateTimeOffset SourceTimestamp,
    long AgeMilliseconds);

public sealed record RuntimeSnapshot(
    long Sequence,
    string Source,
    DateTimeOffset ServerTimestamp,
    IReadOnlyList<RuntimeSnapshotValue> Values,
    RuntimeSourceStatus SourceStatus);

public interface IRuntimeVariableSink
{
    void Apply(RuntimeSnapshot snapshot);
}
