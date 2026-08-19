namespace Scada.Core;

#pragma warning disable CA1720 // Stable schema names intentionally match PLC/.NET data type names.
public enum VariableDataType
{
    Bool,
    Int32,
    UInt32,
    Float64,
    String
}
#pragma warning restore CA1720

public enum VariableDirection
{
    Feedback,
    Command,
    Parameter
}

public enum VariableQuality
{
    Good,
    Uncertain,
    Bad
}

public sealed record VariableDefinition
{
    public VariableDefinition(
        string key,
        VariableDataType dataType,
        VariableDirection direction,
        string? unit = null,
        double? minimum = null,
        double? maximum = null)
    {
        ValidateKey(key);

        if (minimum > maximum)
        {
            throw new ArgumentException("Variable minimum must not exceed its maximum.", nameof(minimum));
        }

        Key = key;
        DataType = dataType;
        Direction = direction;
        Unit = unit;
        Minimum = minimum;
        Maximum = maximum;
    }

    public string Key { get; }

    public VariableDataType DataType { get; }

    public VariableDirection Direction { get; }

    public string? Unit { get; }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public static VariableDefinition Bool(string key, VariableDirection direction) =>
        new(key, VariableDataType.Bool, direction);

    public static VariableDefinition Number(
        string key,
        VariableDataType dataType,
        VariableDirection direction,
        string? unit = null,
        double? minimum = null,
        double? maximum = null)
    {
        if (dataType is VariableDataType.Bool or VariableDataType.String)
        {
            throw new ArgumentException("A numeric variable must use a numeric data type.", nameof(dataType));
        }

        return new VariableDefinition(key, dataType, direction, unit, minimum, maximum);
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var character in key)
        {
            var supported = character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '_' or '.' or '-';

            if (!supported)
            {
                throw new ArgumentException(
                    "Variable keys may contain only ASCII letters, digits, underscore, period and hyphen.",
                    nameof(key));
            }
        }
    }
}

public sealed record VariableValue(
    VariableDataType DataType,
    object? Value,
    VariableQuality Quality,
    DateTimeOffset Timestamp)
{
    public static VariableValue Unknown(VariableDataType type, DateTimeOffset timestamp) =>
        new(type, null, VariableQuality.Bad, timestamp);
}
