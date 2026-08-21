using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed class PropertyPanelViewModel
{
    private readonly EditorSession _session;

    public PropertyPanelViewModel(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
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
}
