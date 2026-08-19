using Scada.Core;

namespace Scada.Simulator;

public sealed class OfflineSimulator
{
    private readonly Dictionary<string, VariableDefinition> _definitions;
    private readonly Dictionary<string, SimulatedFeedback> _overrides = new(StringComparer.Ordinal);
    private readonly uint _seed;

    public OfflineSimulator(IReadOnlyList<VariableDefinition> definitions, uint seed)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var duplicate = definitions
            .GroupBy(definition => definition.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate variable key '{duplicate}' is not allowed.", nameof(definitions));
        }

        _definitions = definitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
        _seed = seed;
    }

    public VariableValue Read(string key, DateTimeOffset now)
    {
        if (!_definitions.TryGetValue(key, out var definition))
        {
            return VariableValue.Unknown(VariableDataType.String, now);
        }

        if (definition.Direction == VariableDirection.Command)
        {
            return VariableValue.Unknown(definition.DataType, now);
        }

        if (_overrides.TryGetValue(key, out var overridden))
        {
            return new VariableValue(definition.DataType, overridden.Value, overridden.Quality, now);
        }

        var value = GenerateValue(definition, now);
        return new VariableValue(definition.DataType, value, VariableQuality.Good, now);
    }

    public void SetFeedback(
        string key,
        object? value,
        VariableQuality quality = VariableQuality.Good)
    {
        if (!_definitions.TryGetValue(key, out var definition))
        {
            throw new KeyNotFoundException($"Variable '{key}' is not defined.");
        }

        if (definition.Direction == VariableDirection.Command)
        {
            throw new InvalidOperationException("Command variables cannot be set as confirmed simulator feedback.");
        }

        ValidateValue(definition, value);
        _overrides[key] = new SimulatedFeedback(value, quality);
    }

    public IReadOnlyDictionary<string, VariableValue> Snapshot(DateTimeOffset now) =>
        _definitions.ToDictionary(pair => pair.Key, pair => Read(pair.Key, now), StringComparer.Ordinal);

    private object GenerateValue(VariableDefinition definition, DateTimeOffset now) =>
        definition.DataType switch
        {
            VariableDataType.Bool => false,
            VariableDataType.Int32 => GenerateInt32(definition, now),
            VariableDataType.UInt32 => GenerateUInt32(definition, now),
            VariableDataType.Float64 => GenerateFloat64(definition, now),
            VariableDataType.String => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(definition))
        };

    private int GenerateInt32(VariableDefinition definition, DateTimeOffset now)
    {
        var minimum = Convert.ToInt32(definition.Minimum ?? 0, System.Globalization.CultureInfo.InvariantCulture);
        var maximum = Convert.ToInt32(definition.Maximum ?? 100, System.Globalization.CultureInfo.InvariantCulture);
        var fraction = GenerateFraction(definition.Key, now);
        return minimum + (int)Math.Floor(fraction * ((long)maximum - minimum + 1));
    }

    private uint GenerateUInt32(VariableDefinition definition, DateTimeOffset now)
    {
        var minimum = Convert.ToUInt32(definition.Minimum ?? 0, System.Globalization.CultureInfo.InvariantCulture);
        var maximum = Convert.ToUInt32(definition.Maximum ?? 100, System.Globalization.CultureInfo.InvariantCulture);
        var fraction = GenerateFraction(definition.Key, now);
        return minimum + (uint)Math.Floor(fraction * ((double)maximum - minimum + 1));
    }

    private double GenerateFloat64(VariableDefinition definition, DateTimeOffset now)
    {
        var minimum = definition.Minimum ?? 0;
        var maximum = definition.Maximum ?? 100;
        return minimum + (GenerateFraction(definition.Key, now) * (maximum - minimum));
    }

    private double GenerateFraction(string key, DateTimeOffset now)
    {
        var hash = 2166136261u ^ _seed;
        foreach (var character in key)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        var ticks = unchecked((ulong)now.UtcTicks);
        hash ^= (uint)ticks;
        hash *= 16777619u;
        hash ^= (uint)(ticks >> 32);
        return hash / ((double)uint.MaxValue + 1);
    }

    private static void ValidateValue(VariableDefinition definition, object? value)
    {
        var validType = definition.DataType switch
        {
            VariableDataType.Bool => value is bool,
            VariableDataType.Int32 => value is int,
            VariableDataType.UInt32 => value is uint,
            VariableDataType.Float64 => value is double,
            VariableDataType.String => value is string,
            _ => false
        };
        if (!validType)
        {
            throw new ArgumentException(
                $"Value type does not match variable data type {definition.DataType}.",
                nameof(value));
        }

        if (value is not null && definition.DataType is VariableDataType.Int32 or VariableDataType.UInt32 or VariableDataType.Float64)
        {
            var numericValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            if (definition.Minimum is not null && numericValue < definition.Minimum
                || definition.Maximum is not null && numericValue > definition.Maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Feedback value is outside the variable range.");
            }
        }
    }

    private sealed record SimulatedFeedback(object? Value, VariableQuality Quality);
}
