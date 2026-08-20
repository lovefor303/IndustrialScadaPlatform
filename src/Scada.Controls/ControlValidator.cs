using Scada.Core;
using Scada.Scene;

namespace Scada.Controls;

public sealed class ControlValidator
{
    public static IReadOnlyList<ProjectValidationError> Validate(
        ControlObject instance,
        ControlDefinition definition,
        IReadOnlyList<VariableDefinition> variables,
        IReadOnlySet<string> availableCapabilities)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(availableCapabilities);

        var result = new List<ProjectValidationError>();
        var variablesByKey = variables.ToDictionary(variable => variable.Key, StringComparer.Ordinal);
        foreach (var role in definition.Bindings)
        {
            if (!instance.Bindings.TryGetValue(role.Name, out var binding))
            {
                if (role.Required)
                {
                    result.Add(Error($"bindings.{role.Name}", "control.binding.required", $"Required binding '{role.Name}' is missing."));
                }

                continue;
            }

            if (!variablesByKey.TryGetValue(binding.VariableKey, out var variable))
            {
                result.Add(Error($"bindings.{role.Name}", "control.binding.missing-variable", $"Variable '{binding.VariableKey}' is not defined."));
                continue;
            }

            if (!role.DataTypes.Contains(variable.DataType))
            {
                result.Add(Error($"bindings.{role.Name}", "control.binding.type", $"Variable '{variable.Key}' has type '{variable.DataType}', which is not allowed for '{role.Name}'."));
            }

            if (variable.Direction != role.Direction)
            {
                result.Add(Error($"bindings.{role.Name}", "control.binding.direction", $"Variable '{variable.Key}' has direction '{variable.Direction}', expected '{role.Direction}'."));
            }

            if (role.UnitFamily is not null && variable.Unit is not null
                && !string.Equals(variable.Unit, role.UnitFamily, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Error($"bindings.{role.Name}", "control.binding.unit", $"Variable '{variable.Key}' unit '{variable.Unit}' does not match '{role.UnitFamily}'."));
            }
        }

        foreach (var interaction in instance.Interactions)
        {
            try
            {
                InteractionCatalog.Event(interaction.Value.EventName);
            }
            catch (KeyNotFoundException)
            {
                result.Add(Error($"interactions.{interaction.Key}.event", "control.event.unknown", $"Unknown event '{interaction.Value.EventName}'."));
            }

            try
            {
                var action = InteractionCatalog.Action(interaction.Value.ActionName);
                if (!availableCapabilities.Contains(action.Id))
                {
                    result.Add(Error($"interactions.{interaction.Key}.action", "control.action.capability-unavailable", $"Action capability '{action.Id}' is unavailable."));
                }
            }
            catch (KeyNotFoundException)
            {
                result.Add(Error($"interactions.{interaction.Key}.action", "control.action.unknown", $"Unknown action '{interaction.Value.ActionName}'."));
            }
        }

        return result;
    }

    private static ProjectValidationError Error(string path, string code, string message) => new(path, code, message);
}
