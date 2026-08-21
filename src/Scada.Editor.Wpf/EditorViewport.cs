using System.Windows;

namespace Scada.Editor.Wpf;

public sealed class EditorViewport
{
    public double Zoom { get; private set; } = 1;

    public Vector Pan { get; private set; }

    public Point ModelToScreen(Point modelPoint) =>
        new(modelPoint.X * Zoom + Pan.X, modelPoint.Y * Zoom + Pan.Y);

    public Point ScreenToModel(Point screenPoint) =>
        new((screenPoint.X - Pan.X) / Zoom, (screenPoint.Y - Pan.Y) / Zoom);

    public void PanBy(Vector delta) => Pan += delta;

    public void SetZoomAt(double requestedZoom, Point screenAnchor)
    {
        var next = Math.Clamp(requestedZoom, 0.1, 8.0);
        var modelAnchor = ScreenToModel(screenAnchor);
        Zoom = next;
        Pan = new Vector(
            screenAnchor.X - modelAnchor.X * Zoom,
            screenAnchor.Y - modelAnchor.Y * Zoom);
    }
}
