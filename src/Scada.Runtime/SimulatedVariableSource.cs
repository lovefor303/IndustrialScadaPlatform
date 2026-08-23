using System.Globalization;
using Scada.Core;

namespace Scada.Runtime;

public sealed class SimulatedVariableSource : IRuntimeVariableSource
{
    private readonly Dictionary<string, VariableDefinition> _definitions;
    private readonly Dictionary<string, RuntimeVariableValue> _values = new(StringComparer.Ordinal);

    public SimulatedVariableSource(IEnumerable<VariableDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var definitionArray = definitions.ToArray();
        var duplicate = definitionArray
            .GroupBy(definition => definition.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"重复变量键“{duplicate.Key}”不允许。", nameof(definitions));
        }

        _definitions = definitionArray.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
    }

    public RuntimeVariableValue Read(string key, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_definitions.TryGetValue(key, out var definition))
        {
            return new RuntimeVariableValue(key, null, VariableDataType.String, VariableQuality.Bad, now);
        }

        return _values.TryGetValue(key, out var value)
            ? value
            : new RuntimeVariableValue(key, null, definition.DataType, VariableQuality.Bad, now);
    }

    public void Set(string key, object? value, VariableQuality quality, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_definitions.TryGetValue(key, out var definition))
        {
            throw new KeyNotFoundException($"变量“{key}”不存在。");
        }

        ValidateValue(definition, value);
        _values[key] = new RuntimeVariableValue(key, value, definition.DataType, quality, timestamp);
    }

    public void SetQuality(string key, VariableQuality quality, DateTimeOffset timestamp)
    {
        if (!_definitions.TryGetValue(key, out var definition))
        {
            throw new KeyNotFoundException($"变量“{key}”不存在。");
        }

        var existing = Read(key, timestamp);
        _values[key] = existing with { Quality = quality, Timestamp = timestamp, DataType = definition.DataType };
    }

    public static SimulatedVariableSource CreateDemo(IEnumerable<VariableDefinition> definitions)
    {
        var source = new SimulatedVariableSource(definitions);
        var timestamp = new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);
        foreach (var definition in definitions)
        {
            object? value = definition.DataType switch
            {
                VariableDataType.Bool => false,
                VariableDataType.Int32 => 0,
                VariableDataType.UInt32 => 0u,
                VariableDataType.Float64 => 0d,
                VariableDataType.String => string.Empty,
                _ => null
            };
            source.Set(definition.Key, value, VariableQuality.Good, timestamp);
        }

        return source;
    }

    private static void ValidateValue(VariableDefinition definition, object? value)
    {
        if (value is null)
        {
            if (definition.DataType != VariableDataType.String)
            {
                throw new ArgumentException($"变量“{definition.Key}”的值不能为空。", nameof(value));
            }

            return;
        }

        var valid = definition.DataType switch
        {
            VariableDataType.Bool => value is bool,
            VariableDataType.Int32 => value is int,
            VariableDataType.UInt32 => value is uint,
            VariableDataType.Float64 => IsNumeric(value),
            VariableDataType.String => value is string,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException(
                $"变量“{definition.Key}”的值类型与声明不匹配。",
                nameof(value));
        }
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
        && double.IsFinite(Convert.ToDouble(value, CultureInfo.InvariantCulture));
}
