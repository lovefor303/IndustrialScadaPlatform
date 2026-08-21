using System.Windows;

namespace Scada.Editor.Wpf;

public sealed class EditorViewport
{
    public double Zoom { get; private set; } = 1;

    public Vector Pan { get; private set; }

    public bool GridEnabled { get; set; } = true;

    public bool RulersEnabled { get; set; } = true;

    public double GridSize { get; set; } = 10;

    public Point ModelToScreen(Point modelPoint) =>
        new(modelPoint.X * Zoom + Pan.X, modelPoint.Y * Zoom + Pan.Y);

    public Point ScreenToModel(Point screenPoint) =>
        new((screenPoint.X - Pan.X) / Zoom, (screenPoint.Y - Pan.Y) / Zoom);

    public void PanBy(Vector delta) => Pan += delta;

    public void SetPan(Vector pan) => Pan = pan;

    public Point SnapToGrid(Point modelPoint)
    {
        if (!GridEnabled)
        {
            return modelPoint;
        }

        var size = GridSize > 0 && double.IsFinite(GridSize) ? GridSize : 1;
        return new Point(
            Math.Round(modelPoint.X / size, MidpointRounding.AwayFromZero) * size,
            Math.Round(modelPoint.Y / size, MidpointRounding.AwayFromZero) * size);
    }

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
