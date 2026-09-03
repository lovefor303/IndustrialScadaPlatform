using Scada.Core;
using Scada.Runtime;

namespace Scada.Gateway;

public sealed class RuntimeSnapshotCache
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RuntimeVariableUpdate> _values = new(StringComparer.Ordinal);
    private readonly List<(long Sequence, RuntimeVariableUpdate Update)> _history = [];
    private readonly TimeSpan _uncertainAfter;
    private readonly TimeSpan _badAfter;
    private readonly string _source;
    private long _sequence;
    private RuntimeSourceStatus _status;

    public RuntimeSnapshotCache(
        string source,
        TimeSpan? uncertainAfter = null,
        TimeSpan? badAfter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        _source = source;
        _uncertainAfter = uncertainAfter ?? TimeSpan.FromSeconds(5);
        _badAfter = badAfter ?? TimeSpan.FromSeconds(20);
        if (_uncertainAfter < TimeSpan.Zero || _badAfter < _uncertainAfter)
        {
            throw new ArgumentOutOfRangeException(nameof(badAfter), "新鲜度阈值必须为非负且坏质量阈值不小于不确定阈值。 ");
        }

        _status = new RuntimeSourceStatus(
            RuntimeSourceState.Stopped,
            source,
            DateTimeOffset.UtcNow,
            null,
            []);
    }

    public RuntimeSourceStatus Status
    {
        get
        {
            lock (_gate)
            {
                return _status;
            }
        }
    }

    public bool Apply(RuntimeVariableUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!string.Equals(update.Source, _source, StringComparison.Ordinal))
        {
            return false;
        }

        lock (_gate)
        {
            if (_values.TryGetValue(update.Value.Key, out var previous)
                && update.Value.Timestamp <= previous.Value.Timestamp)
            {
                return false;
            }

            _values[update.Value.Key] = update;
            _sequence++;
            _history.Add((_sequence, update));
            if (update.Value.Quality != VariableQuality.Bad)
            {
                _status = _status with { LastSuccessfulRead = update.Value.Timestamp };
            }

            return true;
        }
    }

    public void SetStatus(RuntimeSourceStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (!string.Equals(status.Source, _source, StringComparison.Ordinal))
        {
            throw new ArgumentException("状态来源与缓存来源不一致。", nameof(status));
        }

        lock (_gate)
        {
            _status = status;
        }
    }

    public RuntimeSnapshot GetFullSnapshot(
        IEnumerable<string> keys,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var keyArray = keys.ToArray();

        lock (_gate)
        {
            var values = keyArray.Select(key => CreateSnapshotValue(key, now)).ToArray();
            return new RuntimeSnapshot(
                _sequence,
                _source,
                now.ToUniversalTime(),
                values,
                _status);
        }
    }

    public RuntimeSnapshot GetIncrementalSnapshot(
        IEnumerable<string> keys,
        long afterSequence,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentOutOfRangeException.ThrowIfNegative(afterSequence);

        var keySet = keys.ToHashSet(StringComparer.Ordinal);
        lock (_gate)
        {
            var values = _history
                .Where(item => item.Sequence > afterSequence && keySet.Contains(item.Update.Value.Key))
                .GroupBy(item => item.Update.Value.Key, StringComparer.Ordinal)
                .Select(group => CreateSnapshotValue(group.Last().Update.Value.Key, now))
                .ToArray();
            return new RuntimeSnapshot(
                _sequence,
                _source,
                now.ToUniversalTime(),
                values,
                _status);
        }
    }

    private RuntimeSnapshotValue CreateSnapshotValue(string key, DateTimeOffset now)
    {
        if (!_values.TryGetValue(key, out var update))
        {
            return new RuntimeSnapshotValue(
                key,
                VariableDataType.String,
                null,
                VariableQuality.Bad,
                now.ToUniversalTime(),
                0);
        }

        var sourceTimestamp = update.Value.Timestamp.ToUniversalTime();
        var age = now.ToUniversalTime() - sourceTimestamp;
        var ageMilliseconds = Math.Max(0, Math.Min(long.MaxValue, (long)age.TotalMilliseconds));
        var quality = update.Value.Quality == VariableQuality.Bad
            ? VariableQuality.Bad
            : age > _badAfter
                ? VariableQuality.Bad
                : age > _uncertainAfter
                    ? VariableQuality.Uncertain
                    : VariableQuality.Good;

        return new RuntimeSnapshotValue(
            key,
            update.Value.DataType,
            update.Value.Value,
            quality,
            sourceTimestamp,
            ageMilliseconds);
    }
}
