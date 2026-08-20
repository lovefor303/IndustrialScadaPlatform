using Scada.Core;

namespace Scada.Controls;

public enum ControlState
{
    Neutral,
    Stopped,
    Active,
    Transition,
    Fault,
    Unknown
}

public enum StateSignalRole
{
    None,
    ActiveFeedback,
    InactiveFeedback,
    FaultFeedback,
    CommandRequest,
    ProcessValue
}

public enum ResizePolicy
{
    PreserveAspectRatio,
    StretchHorizontal,
    StretchVertical,
    Free
}

public sealed record ControlSize
{
    public ControlSize(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Control dimensions must be finite and positive.");
        }

        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }
}

public sealed record ControlPropertyDefinition
{
    public ControlPropertyDefinition(string name, string defaultValue, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(defaultValue);
        Name = name;
        DefaultValue = defaultValue;
        Required = required;
    }

    public string Name { get; }

    public string DefaultValue { get; }

    public bool Required { get; }
}

public sealed record ControlBindingRole
{
    public ControlBindingRole(
        string name,
        IReadOnlySet<VariableDataType> dataTypes,
        VariableDirection direction,
        StateSignalRole stateRole,
        bool required,
        string? unitFamily = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dataTypes);
        if (dataTypes.Count == 0)
        {
            throw new ArgumentException("A binding role must allow at least one data type.", nameof(dataTypes));
        }

        Name = name;
        DataTypes = dataTypes;
        Direction = direction;
        StateRole = stateRole;
        Required = required;
        UnitFamily = unitFamily;
    }

    public string Name { get; }

    public IReadOnlySet<VariableDataType> DataTypes { get; }

    public VariableDirection Direction { get; }

    public StateSignalRole StateRole { get; }

    public bool Required { get; }

    public string? UnitFamily { get; }
}

public sealed record AnimationDefinition
{
    public AnimationDefinition(string name, string triggerRole, bool stopsOnBadQuality, bool supportsReducedMotion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerRole);
        Name = name;
        TriggerRole = triggerRole;
        StopsOnBadQuality = stopsOnBadQuality;
        SupportsReducedMotion = supportsReducedMotion;
    }

    public string Name { get; }

    public string TriggerRole { get; }

    public bool StopsOnBadQuality { get; }

    public bool SupportsReducedMotion { get; }
}

public sealed record ControlDefinition
{
    public ControlDefinition(
        string typeId,
        int version,
        string displayNameKey,
        ControlSize defaultSize,
        ControlSize minimumSize,
        ResizePolicy resizePolicy,
        IReadOnlyList<ControlPropertyDefinition> properties,
        IReadOnlyList<ControlBindingRole> bindings,
        IReadOnlyList<AnimationDefinition> animations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameKey);
        ArgumentNullException.ThrowIfNull(defaultSize);
        ArgumentNullException.ThrowIfNull(minimumSize);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(animations);

        EnsureUnique(properties.Select(item => item.Name), nameof(properties));
        EnsureUnique(bindings.Select(item => item.Name), nameof(bindings));
        EnsureUnique(animations.Select(item => item.Name), nameof(animations));
        TypeId = typeId;
        Version = version;
        DisplayNameKey = displayNameKey;
        DefaultSize = defaultSize;
        MinimumSize = minimumSize;
        ResizePolicy = resizePolicy;
        Properties = properties;
        Bindings = bindings;
        Animations = animations;
    }

    public string TypeId { get; }

    public int Version { get; }

    public string DisplayNameKey { get; }

    public ControlSize DefaultSize { get; }

    public ControlSize MinimumSize { get; }

    public ResizePolicy ResizePolicy { get; }

    public IReadOnlyList<ControlPropertyDefinition> Properties { get; }

    public IReadOnlyList<ControlBindingRole> Bindings { get; }

    public IReadOnlyList<AnimationDefinition> Animations { get; }

    public ControlBindingRole GetBinding(string name) =>
        Bindings.SingleOrDefault(binding => string.Equals(binding.Name, name, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"Binding role '{name}' was not found on control '{TypeId}' version {Version}.");

    private static void EnsureUnique(IEnumerable<string> names, string parameterName)
    {
        var duplicate = names.GroupBy(name => name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate name '{duplicate.Key}' is not allowed.", parameterName);
        }
    }
}

public static class ControlTypeIds
{
    public const string CentrifugalPump = "equipment.pump.centrifugal";
    public const string AutomatedValve = "equipment.valve.automated";
    public const string Vessel = "equipment.vessel";
    public const string Agitator = "equipment.agitator";
    public const string Filter = "equipment.filter";
    public const string StraightPipe = "pipe.straight";
    public const string PipeElbow = "pipe.elbow";
    public const string PipeTee = "pipe.tee";
    public const string NumericDisplay = "instrument.numeric";
    public const string LevelBar = "instrument.level-bar";
    public const string TemperatureIndicator = "instrument.temperature";
    public const string PressureIndicator = "instrument.pressure";
    public const string FlowIndicator = "instrument.flow";
    public const string CommandButton = "operator.command-button";

    public static IReadOnlyList<string> All { get; } =
    [
        CentrifugalPump, AutomatedValve, Vessel, Agitator, Filter,
        StraightPipe, PipeElbow, PipeTee, NumericDisplay, LevelBar,
        TemperatureIndicator, PressureIndicator, FlowIndicator, CommandButton
    ];
}
