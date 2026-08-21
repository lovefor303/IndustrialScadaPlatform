using Scada.Core;
using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed class DynamicsPanelViewModel
{
    private readonly EditorSession _session;

    public DynamicsPanelViewModel(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public bool TrySet(DynamicDefinition definition, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var validation = new List<string>();
        var selected = _session.SelectedObjectIds.Count == 1
            ? _session.ActiveScreen.FindObject(_session.SelectedObjectIds.Single())
            : null;
        if (selected is null)
        {
            validation.Add("对象选择 / dynamics: 请选择一个画面对象。");
        }
        else
        {
            var variable = _session.Project.Variables.SingleOrDefault(item =>
                string.Equals(item.Key, definition.VariableKey, StringComparison.Ordinal));
            if (variable is null)
            {
                validation.Add($"对象 {selected.Id} / dynamics.variable: 变量“{definition.VariableKey}”不存在。");
            }
            else
            {
                if (variable.DataType != definition.ExpectedDataType)
                {
                    validation.Add($"对象 {selected.Id} / dynamics.dataType: 动态变量“{definition.VariableKey}”类型不匹配。");
                }

                if (variable.Direction != definition.ExpectedDirection)
                {
                    validation.Add($"对象 {selected.Id} / dynamics.direction: 动态变量“{definition.VariableKey}”方向不匹配。");
                }
            }
        }

        if (validation.Count == 0)
        {
            _session.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Dynamics = sceneObject.Dynamics.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal)
                    .SetItem(definition.TargetProperty, definition)
            });
        }

        errors = validation;
        return validation.Count == 0;
    }
}
