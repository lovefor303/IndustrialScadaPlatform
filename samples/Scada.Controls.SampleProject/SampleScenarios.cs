using Scada.Controls.Rendering;
using Scada.Core;
using Scada.Scene;
using Scada.Simulator;

namespace Scada.Controls.SampleProject;

public sealed record PendingPreviewAction(Guid TargetObjectId, string ActionId);

public sealed record SampleScenario(
    string Name,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, VariableValue> Feedback,
    IReadOnlySet<PendingPreviewAction> PendingActions)
{
    public ControlRenderContext CreateRenderContext(SceneObject control, bool reducedMotion)
    {
        ArgumentNullException.ThrowIfNull(control);
        var definition = ControlCatalog.CreateDefault().Get(control.Type, control.ControlVersion);
        var roleValues = new Dictionary<string, VariableValue>(StringComparer.Ordinal);
        foreach (var role in definition.Bindings)
        {
            if (role.Direction == VariableDirection.Command)
            {
                var pending = PendingActions.Any(action => action.TargetObjectId == control.Id);
                roleValues.Add(role.Name, new VariableValue(VariableDataType.Bool, pending, VariableQuality.Good, Timestamp));
                continue;
            }

            if (control.Bindings.TryGetValue(role.Name, out var binding) && Feedback.TryGetValue(binding.VariableKey, out var value))
            {
                roleValues.Add(role.Name, value);
            }
        }

        var resolution = ControlStateResolver.Resolve(definition, roleValues, Timestamp);
        var numeric = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["Minimum"] = 0,
            ["Maximum"] = 100
        };
        foreach (var pair in roleValues.Where(pair => pair.Value.Value is not null))
        {
            if (pair.Value.Value is int integer)
            {
                numeric[pair.Key] = integer;
            }
            else if (pair.Value.Value is uint unsignedInteger)
            {
                numeric[pair.Key] = unsignedInteger;
            }
            else if (pair.Value.Value is double doubleValue)
            {
                numeric[pair.Key] = doubleValue;
            }
        }

        if (!numeric.ContainsKey("ProcessValue"))
        {
            numeric["ProcessValue"] = 0;
        }

        var unit = control.Type switch
        {
            ControlTypeIds.TemperatureIndicator => "°C",
            ControlTypeIds.PressureIndicator => "bar",
            ControlTypeIds.FlowIndicator => "L/min",
            ControlTypeIds.LevelBar => "%",
            _ => string.Empty
        };
        return ControlRenderContext.ForState(resolution.State) with
        {
            ReducedMotion = reducedMotion,
            Quality = roleValues.Values.Any(value => value.Quality != VariableQuality.Good)
                ? VariableQuality.Bad
                : VariableQuality.Good,
            NumericValues = numeric,
            TextValues = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Label"] = control.Properties.TryGetValue("Label", out var label) ? label : control.Type,
                ["Unit"] = unit,
                ["ActionKind"] = PendingActions.Any(action => action.TargetObjectId == control.Id) ? "pending-command" : "write-command",
                ["ActionTarget"] = control.Bindings.Values.FirstOrDefault()?.VariableKey ?? "sample.action.none"
            }
        };
    }
}

public static class SampleScenarios
{
    public static readonly DateTimeOffset ReviewTimestamp = new(2026, 8, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly Dictionary<string, SampleScenario> Scenarios = CreateAll();

    public static SampleScenario Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Scenarios.TryGetValue(name, out var scenario)
            ? scenario
            : throw new KeyNotFoundException($"Sample scenario '{name}' is not registered.");
    }

    private static Dictionary<string, SampleScenario> CreateAll() =>
        new Dictionary<string, SampleScenario>(StringComparer.Ordinal)
        {
            ["stopped"] = Create("stopped", ScenarioMode.Stopped),
            ["active"] = Create("active", ScenarioMode.Active),
            ["transition"] = Create("transition", ScenarioMode.Transition),
            ["fault"] = Create("fault", ScenarioMode.Fault),
            ["unknown"] = Create("unknown", ScenarioMode.Unknown)
        };

    private static SampleScenario Create(string name, ScenarioMode mode)
    {
        var project = SampleProjectFactory.Create();
        var simulator = new OfflineSimulator(project.Variables, seed: 20260820);
        foreach (var variable in project.Variables.Where(variable => variable.Direction != VariableDirection.Command))
        {
            simulator.SetFeedback(variable.Key, FeedbackValue(variable, mode), mode == ScenarioMode.Unknown ? VariableQuality.Bad : VariableQuality.Good);
        }

        var pendingActions = mode == ScenarioMode.Transition
            ? new HashSet<PendingPreviewAction>
            {
                new(Guid.Parse("10000000-0000-0000-0000-000000000012"), "command.write"),
                new(Guid.Parse("10000000-0000-0000-0000-000000000024"), "command.write")
            }
            : new HashSet<PendingPreviewAction>();
        var feedback = simulator.Snapshot(ReviewTimestamp)
            .Where(pair => project.Variables.Single(variable => variable.Key == pair.Key).Direction != VariableDirection.Command)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return new SampleScenario(name, ReviewTimestamp, feedback, pendingActions);
    }

    private static object FeedbackValue(VariableDefinition definition, ScenarioMode mode)
    {
        if (definition.DataType == VariableDataType.Bool)
        {
            if (definition.Key.Contains("faultFeedback", StringComparison.Ordinal))
            {
                return mode == ScenarioMode.Fault;
            }

            if (definition.Key.Contains("runFeedback", StringComparison.Ordinal) || definition.Key.Contains("openFeedback", StringComparison.Ordinal))
            {
                return mode == ScenarioMode.Active;
            }

            if (definition.Key.Contains("stopFeedback", StringComparison.Ordinal) || definition.Key.Contains("closedFeedback", StringComparison.Ordinal))
            {
                return mode == ScenarioMode.Stopped;
            }

            return false;
        }

        return definition.Key switch
        {
            var key when key.Contains("level", StringComparison.Ordinal) => 64d,
            var key when key.Contains("temperature", StringComparison.Ordinal) => 46.8d,
            var key when key.Contains("pressure", StringComparison.Ordinal) => 2.35d,
            var key when key.Contains("flow", StringComparison.Ordinal) => 18.4d,
            var key when key.Contains("position", StringComparison.Ordinal) => mode == ScenarioMode.Active ? 82d : 0d,
            var key when key.Contains("numeric", StringComparison.Ordinal) => 248d,
            _ => 0d
        };
    }

    private enum ScenarioMode
    {
        Stopped,
        Active,
        Transition,
        Fault,
        Unknown
    }
}
