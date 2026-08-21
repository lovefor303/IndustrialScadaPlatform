using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Scada.Editor.Wpf;

public sealed class EditorShellViewModel : INotifyPropertyChanged
{
    private string _statusText = "就绪";

    public EditorShellViewModel(EditorRole role = EditorRole.Developer)
    {
        Role = role;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorRole Role { get; }

    public bool IsEngineeringEnabled => EditorAuthorization.CanEdit(Role);

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
