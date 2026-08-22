using Scada.Core;
using Scada.Scene;
using System.Windows.Input;
using System.ComponentModel;

namespace Scada.Editor.Wpf;

public sealed class DynamicsPanelViewModel : INotifyPropertyChanged
{
    private readonly EditorSession _session;

    public DynamicsPanelViewModel(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        ApplyCommand = new EditorCommand(Apply, CanApply);
    }

    public string TargetProperty { get; set; } = string.Empty;
    public string VariableKey { get; set; } = string.Empty;
    public VariableDataType ExpectedDataType { get; set; } = VariableDataType.Float64;
    public VariableDirection ExpectedDirection { get; set; } = VariableDirection.Feedback;
    public string? Condition { get; set; }
    public string ErrorText { get; private set; } = string.Empty;
    public ICommand ApplyCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableVariables)));
        (ApplyCommand as EditorCommand)?.Refresh();
    }

    public IReadOnlyList<string> AvailableTargets { get; } = ["Visibility", "Rotation", "Value"];

    public IReadOnlyList<string> AvailableVariables => _session.Project.Variables.Select(item => item.Key).ToArray();

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

    private bool CanApply() => _session.SelectedObjectIds.Count == 1
        && !string.IsNullOrWhiteSpace(TargetProperty)
        && !string.IsNullOrWhiteSpace(VariableKey);

    private void Apply()
    {
        var definition = new DynamicDefinition(TargetProperty, VariableKey, ExpectedDataType, ExpectedDirection, Condition);
        TrySet(definition, out var errors);
        ErrorText = string.Join("；", errors);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorText)));
    }
}
