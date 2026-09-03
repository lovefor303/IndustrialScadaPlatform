using Scada.Core;

namespace Scada.Controls;

public sealed record ControlStateResult(
    ControlState State,
    string ReasonCode,
    IReadOnlySet<string> ActiveAnimations);

public static class ControlStateResolver
{
    public static ControlStateResult Resolve(
        ControlDefinition definition,
        IReadOnlyDictionary<string, VariableValue> roleValues,
        DateTimeOffset now,
        TimeSpan? staleAfter = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(roleValues);
        var staleLimit = staleAfter ?? TimeSpan.FromSeconds(5);
        if (staleLimit < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(staleAfter), "Staleness duration cannot be negative.");
        }

        var requiredFeedback = definition.Bindings
            .Where(binding => binding.Required && binding.Direction == VariableDirection.Feedback)
            .ToArray();
        if (requiredFeedback.Any(binding => !roleValues.TryGetValue(binding.Name, out var value)
            || value.Quality == VariableQuality.Bad
            || now - value.Timestamp > staleLimit
            || now < value.Timestamp))
        {
            return Result(ControlState.Unknown, "quality.invalid", EmptyAnimations());
        }

        var fault = ReadBool(roleValues, definition, StateSignalRole.FaultFeedback);
        var active = ReadBool(roleValues, definition, StateSignalRole.ActiveFeedback);
        var inactive = ReadBool(roleValues, definition, StateSignalRole.InactiveFeedback);
        if (fault || (active && inactive))
        {
            return Result(ControlState.Fault, fault ? "feedback.fault" : "feedback.contradictory", EmptyAnimations());
        }

        var commandPending = definition.Bindings
            .Where(binding => binding.StateRole == StateSignalRole.CommandRequest)
            .Select(binding => roleValues.TryGetValue(binding.Name, out var value) && value.Quality == VariableQuality.Good && value.Value is bool flag && flag)
            .Any(value => value);
        if (commandPending && !active && !inactive)
        {
            return Result(ControlState.Transition, "command.awaiting-feedback", EmptyAnimations());
        }

        if (active)
        {
            return Result(ControlState.Active, "feedback.active", AnimationsFor(definition, roleValues));
        }

        if (inactive)
        {
            return Result(ControlState.Stopped, "feedback.inactive", EmptyAnimations());
        }

        return Result(ControlState.Neutral, "feedback.neutral", EmptyAnimations());
    }

    private static bool ReadBool(
        IReadOnlyDictionary<string, VariableValue> roleValues,
        ControlDefinition definition,
        StateSignalRole role)
    {
        var names = definition.Bindings.Where(binding => binding.StateRole == role).Select(binding => binding.Name);
        return names.Any(name => roleValues.TryGetValue(name, out var value)
            && value.Quality == VariableQuality.Good
            && value.DataType == VariableDataType.Bool
            && value.Value is bool flag
            && flag);
    }

    private static HashSet<string> AnimationsFor(
        ControlDefinition definition,
        IReadOnlyDictionary<string, VariableValue> roleValues) =>
        definition.Animations
            .Where(animation => roleValues.TryGetValue(animation.TriggerRole, out var value)
                && value.Quality == VariableQuality.Good)
            .Select(animation => animation.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static ControlStateResult Result(ControlState state, string reasonCode, IReadOnlySet<string> animations) =>
        new(state, reasonCode, animations);

    private static HashSet<string> EmptyAnimations() => new(StringComparer.Ordinal);
}
