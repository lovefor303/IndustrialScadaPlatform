using Scada.Controls;
using Scada.Core;
using Scada.Scene;
using System.Windows.Input;
using System.ComponentModel;

namespace Scada.Editor.Wpf;

public sealed class BindingPanelViewModel : INotifyPropertyChanged
{
    private readonly EditorSession _session;
    private readonly ControlCatalog _catalog;

    public BindingPanelViewModel(EditorSession session, ControlCatalog? catalog = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _catalog = catalog ?? ControlCatalog.CreateDefault();
        ApplyCommand = new EditorCommand(Apply, CanApply);
    }

    public string RoleName { get; set; } = string.Empty;
    public string VariableKey { get; set; } = string.Empty;
    public string ErrorText { get; private set; } = string.Empty;
    public ICommand ApplyCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRoles)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableVariables)));
        (ApplyCommand as EditorCommand)?.Refresh();
    }

    public IReadOnlyList<string> AvailableRoles => _session.SelectedObjectIds.Count == 1
        && _session.ActiveScreen.FindObject(_session.SelectedObjectIds.Single()) is ControlObject control
        ? _catalog.Get(control.Type, control.ControlVersion).Bindings.Select(item => item.Name).ToArray()
        : Array.Empty<string>();

    public IReadOnlyList<string> AvailableVariables => _session.Project.Variables.Select(item => item.Key).ToArray();

    public bool TrySet(string roleName, string variableKey, out IReadOnlyList<string> errors)
    {
        var validation = new List<string>();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            validation.Add("变量角色不能为空。");
        }

        if (string.IsNullOrWhiteSpace(variableKey))
        {
            validation.Add("变量不能为空。");
        }

        var selected = _session.SelectedObjectIds.Count == 1
            ? _session.ActiveScreen.FindObject(_session.SelectedObjectIds.Single())
            : null;
        if (selected is not ControlObject control)
        {
            validation.Add("请选择一个控件对象。");
        }
        else
        {
            var objectId = control.Id;
            ControlBindingRole? role = null;
            try
            {
                role = _catalog.Get(control.Type, control.ControlVersion).GetBinding(roleName);
            }
            catch (KeyNotFoundException)
            {
                validation.Add($"对象 {objectId} / bindings.{roleName}: 控件不存在变量角色“{roleName}”。");
            }

            var variable = _session.Project.Variables.SingleOrDefault(item =>
                string.Equals(item.Key, variableKey, StringComparison.Ordinal));
            if (variable is null)
            {
                validation.Add($"对象 {objectId} / bindings.{roleName}.variable: 变量“{variableKey}”不存在。");
            }
            else if (role is not null)
            {
                if (!role.DataTypes.Contains(variable.DataType))
                {
                    validation.Add($"对象 {objectId} / bindings.{roleName}.type: 变量“{variableKey}”类型与角色“{roleName}”不匹配。");
                }

                if (variable.Direction != role.Direction)
                {
                    validation.Add($"对象 {objectId} / bindings.{roleName}.direction: 变量“{variableKey}”方向与角色“{roleName}”不匹配。");
                }
            }

            if (validation.Count == 0)
            {
                _session.UpdateSelectedObject(sceneObject => sceneObject with
                {
                    Bindings = sceneObject.Bindings.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.Ordinal)
                        .SetItem(roleName, new BindingDefinition(variableKey, roleName))
                });
            }
        }

        errors = validation;
        return validation.Count == 0;
    }

    private bool CanApply() => _session.SelectedObjectIds.Count == 1
        && !string.IsNullOrWhiteSpace(RoleName)
        && !string.IsNullOrWhiteSpace(VariableKey);

    private void Apply()
    {
        TrySet(RoleName, VariableKey, out var errors);
        ErrorText = string.Join("；", errors);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorText)));
    }
}

internal static class BindingDictionaryExtensions
{
    public static Dictionary<TKey, TValue> SetItem<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        dictionary[key] = value;
        return dictionary;
    }
}
