namespace Scada.Runtime;

public sealed class RuntimeSnapshotApplier
{
    private readonly IRuntimeVariableSink _sink;
    private readonly HashSet<string> _knownKeys;
    private long _lastSequence;

    public RuntimeSnapshotApplier(
        IRuntimeVariableSink sink,
        IEnumerable<string> knownKeys)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(knownKeys);

        _sink = sink;
        _knownKeys = knownKeys.ToHashSet(StringComparer.Ordinal);
    }

    public long LastSequence => _lastSequence;

    public bool Apply(RuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Sequence <= _lastSequence)
        {
            return false;
        }

        var filteredValues = snapshot.Values
            .Where(value => _knownKeys.Contains(value.Key))
            .ToArray();
        _lastSequence = snapshot.Sequence;
        _sink.Apply(snapshot with { Values = filteredValues });
        return true;
    }
}
