using Scada.Core;

namespace Scada.Controls;

public sealed class ControlCatalog
{
    private readonly Dictionary<(string TypeId, int Version), ControlDefinition> _definitions;

    private ControlCatalog(IEnumerable<ControlDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(definition => (definition.TypeId, definition.Version));
    }

    public static ControlCatalog CreateDefault()
    {
        var boolFeedback = new HashSet<VariableDataType> { VariableDataType.Bool };
        var numeric = new HashSet<VariableDataType>
        {
            VariableDataType.Int32, VariableDataType.UInt32, VariableDataType.Float64
        };
        var definitions = new[]
        {
            Equipment(ControlTypeIds.CentrifugalPump, "control.pump", 120, 72,
                [Binding("StartCommand", boolFeedback, VariableDirection.Command, StateSignalRole.CommandRequest, false),
                 Binding("RunFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.ActiveFeedback, true),
                 Binding("StopFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.InactiveFeedback, false),
                 Binding("FaultFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.FaultFeedback, false)],
                [new AnimationDefinition("pump.rotate", "RunFeedback", true, true)]),
            Equipment(ControlTypeIds.AutomatedValve, "control.valve", 100, 80,
                [Binding("OpenCommand", boolFeedback, VariableDirection.Command, StateSignalRole.CommandRequest, false),
                 Binding("CloseCommand", boolFeedback, VariableDirection.Command, StateSignalRole.CommandRequest, false),
                 Binding("OpenFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.ActiveFeedback, true),
                 Binding("ClosedFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.InactiveFeedback, true),
                 Binding("FaultFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.FaultFeedback, false),
                 Binding("Position", numeric, VariableDirection.Feedback, StateSignalRole.ProcessValue, false)],
                [new AnimationDefinition("valve.travel", "Position", true, true)]),
            Equipment(ControlTypeIds.Vessel, "control.vessel", 180, 240,
                [Binding("LevelValue", numeric, VariableDirection.Feedback, StateSignalRole.ProcessValue, false, "level"),
                 Binding("TemperatureValue", numeric, VariableDirection.Feedback, StateSignalRole.ProcessValue, false, "temperature")], []),
            Equipment(ControlTypeIds.Agitator, "control.agitator", 100, 160,
                [Binding("RunFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.ActiveFeedback, false),
                 Binding("FaultFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.FaultFeedback, false)],
                [new AnimationDefinition("agitator.rotate", "RunFeedback", true, true)]),
            Equipment(ControlTypeIds.Filter, "control.filter", 100, 160,
                [Binding("DifferentialPressure", numeric, VariableDirection.Feedback, StateSignalRole.ProcessValue, false, "pressure"),
                 Binding("FaultFeedback", boolFeedback, VariableDirection.Feedback, StateSignalRole.FaultFeedback, false)], []),
            Equipment(ControlTypeIds.StraightPipe, "control.pipe.straight", 120, 20, [], [new AnimationDefinition("pipe.flow", "FlowFeedback", true, true)], ResizePolicy.StretchHorizontal),
            Equipment(ControlTypeIds.PipeElbow, "control.pipe.elbow", 60, 60, [], [], ResizePolicy.Free),
            Equipment(ControlTypeIds.PipeTee, "control.pipe.tee", 80, 80, [], [], ResizePolicy.Free),
            Measurement(ControlTypeIds.NumericDisplay, "control.instrument.numeric", numeric),
            Measurement(ControlTypeIds.LevelBar, "control.instrument.level-bar", numeric, "level"),
            Measurement(ControlTypeIds.TemperatureIndicator, "control.instrument.temperature", numeric, "temperature"),
            Measurement(ControlTypeIds.PressureIndicator, "control.instrument.pressure", numeric, "pressure"),
            Measurement(ControlTypeIds.FlowIndicator, "control.instrument.flow", numeric, "flow"),
            Equipment(ControlTypeIds.CommandButton, "control.operator.command-button", 120, 40,
                [Binding("Command", boolFeedback, VariableDirection.Command, StateSignalRole.CommandRequest, true)], [])
        };
        return new ControlCatalog(definitions);
    }

    public ControlDefinition Get(string typeId, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        return _definitions.TryGetValue((typeId, version), out var definition)
            ? definition
            : throw new KeyNotFoundException($"Control '{typeId}' version {version} is not registered.");
    }

    private static ControlDefinition Equipment(
        string typeId,
        string displayName,
        double width,
        double height,
        IReadOnlyList<ControlBindingRole> bindings,
        IReadOnlyList<AnimationDefinition> animations,
        ResizePolicy resizePolicy = ResizePolicy.PreserveAspectRatio) =>
        new(typeId, 1, displayName, new(width, height), new(Math.Min(width, 24), Math.Min(height, 24)), resizePolicy, [], bindings, animations);

    private static ControlDefinition Measurement(
        string typeId,
        string displayName,
        IReadOnlySet<VariableDataType> numeric,
        string? unitFamily = null) =>
        Equipment(typeId, displayName, 120, 48,
            [Binding("ProcessValue", numeric, VariableDirection.Feedback, StateSignalRole.ProcessValue, true, unitFamily)], []);

    private static ControlBindingRole Binding(
        string name,
        IReadOnlySet<VariableDataType> dataTypes,
        VariableDirection direction,
        StateSignalRole stateRole,
        bool required,
        string? unitFamily = null) =>
        new(name, dataTypes, direction, stateRole, required, unitFamily);
}
