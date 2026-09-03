using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed class PropertyPanelViewModel : INotifyPropertyChanged
{
    private readonly EditorSession _session;
    private string _x = string.Empty;
    private string _y = string.Empty;
    private string _width = string.Empty;
    private string _height = string.Empty;
    private string _rotation = string.Empty;
    private bool _isVisible = true;

    public PropertyPanelViewModel(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        ApplyCommand = new EditorCommand(Apply, CanApply);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string X { get => _x; set => SetField(ref _x, value); }
    public string Y { get => _y; set => SetField(ref _y, value); }
    public string Width { get => _width; set => SetField(ref _width, value); }
    public string Height { get => _height; set => SetField(ref _height, value); }
    public string Rotation { get => _rotation; set => SetField(ref _rotation, value); }
    public bool IsVisible { get => _isVisible; set => SetField(ref _isVisible, value); }
    public ICommand ApplyCommand { get; }

    public void Refresh()
    {
        if (_session.SelectedObjectIds.Count != 1)
        {
            X = Y = Width = Height = Rotation = string.Empty;
            IsVisible = true;
            RefreshCommand();
            return;
        }

        var selected = _session.ActiveScreen.FindObject(_session.SelectedObjectIds.Single());
        if (selected is null)
        {
            return;
        }

        X = selected.Bounds.X.ToString(CultureInfo.InvariantCulture);
        Y = selected.Bounds.Y.ToString(CultureInfo.InvariantCulture);
        Width = selected.Bounds.Width.ToString(CultureInfo.InvariantCulture);
        Height = selected.Bounds.Height.ToString(CultureInfo.InvariantCulture);
        Rotation = selected.Rotation.ToString(CultureInfo.InvariantCulture);
        IsVisible = selected.IsVisible;
        RefreshCommand();
    }

    public void SetGeometry(RectD bounds, double rotation, bool isVisible)
    {
        if (!double.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }

        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y)
            || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)
            || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "对象尺寸必须为有限的正数。");
        }

        _session.UpdateSelectedObject(sceneObject => sceneObject with
        {
            Bounds = bounds,
            Rotation = rotation,
            IsVisible = isVisible
        });
    }

    private bool CanApply()
    {
        if (_session.SelectedObjectIds.Count != 1
            || !TryParse(X, out _)
            || !TryParse(Y, out _)
            || !TryParse(Width, out var width)
            || !TryParse(Height, out var height)
            || !TryParse(Rotation, out _))
        {
            return false;
        }

        return width > 0 && height > 0;
    }

    private void Apply()
    {
        if (!CanApply())
        {
            return;
        }

        if (!TryParse(X, out var x) || !TryParse(Y, out var y)
            || !TryParse(Width, out var width) || !TryParse(Height, out var height)
            || !TryParse(Rotation, out var rotation))
        {
            throw new FormatException("位置、尺寸和旋转角度必须是有效数字。");
        }

        SetGeometry(new RectD(x, y, width, height), rotation, IsVisible);
    }

    private static bool TryParse(string value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private void RefreshCommand() =>
        (ApplyCommand as EditorCommand)?.Refresh();

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        RefreshCommand();
    }
}
