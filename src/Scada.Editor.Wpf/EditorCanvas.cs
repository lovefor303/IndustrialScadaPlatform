using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Wpf;
using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed class EditorCanvas : Canvas
{
    private const string ToolboxDataFormat = "Scada.Editor.ToolboxType";
    private bool _isPanning;
    private Point _panStart;
    private Vector _panOrigin;

    private enum HandlePosition
    {
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }

    private sealed record ResizeHandleTag(Guid ObjectId, HandlePosition Position);

    public EditorCanvas()
    {
        Background = new SolidColorBrush(Color.FromRgb(23, 27, 31));
        Focusable = true;
        AllowDrop = true;
        DragOver += OnDragOver;
        Drop += OnDrop;
        MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
        KeyDown += OnKeyDown;
        MouseWheel += OnMouseWheel;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    public EditorSession? Session { get; set; }

    public EditorViewport Viewport { get; } = new();

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!Viewport.GridEnabled || Viewport.GridSize <= 0 || !double.IsFinite(Viewport.GridSize))
        {
            return;
        }

        var spacing = Viewport.GridSize * Viewport.Zoom;
        if (spacing < 4)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Color.FromRgb(42, 49, 56)), 1);
        for (var x = Viewport.Pan.X % spacing; x < ActualWidth; x += spacing)
        {
            dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }

        for (var y = Viewport.Pan.Y % spacing; y < ActualHeight; y += spacing)
        {
            dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }
    }

    public void Refresh()
    {
        Children.Clear();
        if (Session is null)
        {
            return;
        }

        foreach (var sceneObject in Session.ActiveScreen.Objects.OrderBy(item => item.ZIndex))
        {
            var visual = CreateVisual(sceneObject);
            Children.Add(visual);
            SetLeft(visual, sceneObject.Bounds.X * Viewport.Zoom + Viewport.Pan.X);
            SetTop(visual, sceneObject.Bounds.Y * Viewport.Zoom + Viewport.Pan.Y);
        }

        foreach (var sceneObject in Session.ActiveScreen.Objects.Where(item =>
                     Session.SelectedObjectIds.Contains(item.Id)
                     && item is not PipeObject))
        {
            AddSelectionHandles(sceneObject);
        }

        InvalidateVisual();
    }

    public static void BeginToolboxDrag(DependencyObject source, string typeId) =>
        DragDrop.DoDragDrop(
            source,
            new DataObject(ToolboxDataFormat, typeId),
            DragDropEffects.Copy);

    private UIElement CreateVisual(SceneObject sceneObject)
    {
        UIElement visual;
        switch (sceneObject)
        {
            case ControlObject control:
                visual = CreateControlVisual(control);
                break;
            case PipeObject pipe:
                visual = CreatePipeVisual(pipe, Viewport.Zoom);
                break;
            case TextObject text:
                visual = new TextBlock
                {
                    Text = text.Text,
                    Foreground = Brushes.Gainsboro,
                    FontSize = 14,
                    Width = text.Bounds.Width,
                    Height = text.Bounds.Height
                };
                break;
            default:
                throw new NotSupportedException($"Scene object '{sceneObject.Type}' is not supported.");
        }

        if (visual is FrameworkElement element)
        {
            element.Tag = sceneObject.Id;
            element.Width = sceneObject.Bounds.Width * Viewport.Zoom;
            element.Height = sceneObject.Bounds.Height * Viewport.Zoom;
            element.RenderTransform = new RotateTransform(sceneObject.Rotation);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.MouseLeftButtonDown += OnObjectMouseLeftButtonDown;
        }

        if (Session!.SelectedObjectIds.Contains(sceneObject.Id))
        {
            visual = WrapSelection(
                visual,
                sceneObject.Bounds.Width * Viewport.Zoom,
                sceneObject.Bounds.Height * Viewport.Zoom,
                sceneObject.Id);
        }

        return visual;
    }

    private static IndustrialControl CreateControlVisual(ControlObject control)
    {
        var plan = ControlGeometryFactory.Build(
            control.Type,
            ControlRenderContext.ForState(ControlState.Neutral));
        return new IndustrialControl
        {
            RenderPlan = plan,
            State = ControlState.Neutral,
            IsHitTestVisible = true
        };
    }

    private static Polyline CreatePipeVisual(PipeObject pipe, double zoom)
    {
        var polyline = new Polyline
        {
            Stroke = Brushes.SlateGray,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = true
        };
        foreach (var point in new[] { pipe.Start }.Concat(pipe.Bends).Append(pipe.End))
        {
            polyline.Points.Add(new Point(
                (point.X - pipe.Bounds.X) * zoom,
                (point.Y - pipe.Bounds.Y) * zoom));
        }

        return polyline;
    }

    private static Border WrapSelection(UIElement content, double width, double height, Guid objectId) =>
        new()
        {
            Width = width,
            Height = height,
            BorderBrush = Brushes.DeepSkyBlue,
            BorderThickness = new Thickness(1),
            Tag = objectId,
            Child = content
        };

    private void AddSelectionHandles(SceneObject sceneObject)
    {
        var bounds = sceneObject.Bounds;
        foreach (var position in Enum.GetValues<HandlePosition>())
        {
            var handle = new Thumb
            {
                Width = 8,
                Height = 8,
                Background = Brushes.White,
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.SizeAll,
                Tag = new ResizeHandleTag(sceneObject.Id, position)
            };
            handle.DragDelta += OnResizeHandleDragDelta;
            Children.Add(handle);
            SetHandlePosition(handle, bounds, position);
        }

        var rotationHandle = new Thumb
        {
            Width = 8,
            Height = 8,
            Background = Brushes.Orange,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Tag = "rotation"
        };
        rotationHandle.DragDelta += (_, args) =>
        {
            if (Session is null)
            {
                return;
            }

            Session.UpdateSelectedObject(selected => selected with
            {
                Rotation = selected.Rotation + args.HorizontalChange
            });
            Refresh();
        };
        Children.Add(rotationHandle);
        SetLeft(rotationHandle, (bounds.X + bounds.Width / 2) * Viewport.Zoom + Viewport.Pan.X - 4);
        SetTop(rotationHandle, (bounds.Y - 20) * Viewport.Zoom + Viewport.Pan.Y);
    }

    private void SetHandlePosition(Thumb handle, RectD bounds, HandlePosition position)
    {
        var x = position switch
        {
            HandlePosition.TopLeft or HandlePosition.Left or HandlePosition.BottomLeft => bounds.X,
            HandlePosition.Top or HandlePosition.Bottom => bounds.X + bounds.Width / 2,
            _ => bounds.X + bounds.Width
        };
        var y = position switch
        {
            HandlePosition.TopLeft or HandlePosition.Top or HandlePosition.TopRight => bounds.Y,
            HandlePosition.Left or HandlePosition.Right => bounds.Y + bounds.Height / 2,
            _ => bounds.Y + bounds.Height
        };
        SetLeft(handle, x * Viewport.Zoom + Viewport.Pan.X - 4);
        SetTop(handle, y * Viewport.Zoom + Viewport.Pan.Y - 4);
    }

    private void OnResizeHandleDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Session is null || sender is not Thumb { Tag: ResizeHandleTag tag })
        {
            return;
        }

        var dx = e.HorizontalChange / Viewport.Zoom;
        var dy = e.VerticalChange / Viewport.Zoom;
        Session.SelectOnly(tag.ObjectId);
        Session.UpdateSelectedObject(sceneObject => sceneObject with
        {
            Bounds = ResizeBounds(sceneObject.Bounds, tag.Position, dx, dy)
        });
        Refresh();
    }

    private static RectD ResizeBounds(RectD bounds, HandlePosition position, double dx, double dy)
    {
        const double minimum = 16;
        var left = bounds.X;
        var top = bounds.Y;
        var right = bounds.X + bounds.Width;
        var bottom = bounds.Y + bounds.Height;
        if (position is HandlePosition.TopLeft or HandlePosition.Left or HandlePosition.BottomLeft)
        {
            left = Math.Min(left + dx, right - minimum);
        }
        if (position is HandlePosition.TopRight or HandlePosition.Right or HandlePosition.BottomRight)
        {
            right = Math.Max(right + dx, left + minimum);
        }
        if (position is HandlePosition.TopLeft or HandlePosition.Top or HandlePosition.TopRight)
        {
            top = Math.Min(top + dy, bottom - minimum);
        }
        if (position is HandlePosition.BottomLeft or HandlePosition.Bottom or HandlePosition.BottomRight)
        {
            bottom = Math.Max(bottom + dy, top + minimum);
        }

        return new RectD(left, top, right - left, bottom - top);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(ToolboxDataFormat)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (Session is null || !e.Data.GetDataPresent(ToolboxDataFormat))
        {
            return;
        }

        var typeId = (string)e.Data.GetData(ToolboxDataFormat)!;
        var entry = ToolboxCatalog.CreateDefault().Get(typeId);
        var modelPoint = Viewport.SnapToGrid(Viewport.ScreenToModel(e.GetPosition(this)));
        var bounds = new RectD(
            modelPoint.X,
            modelPoint.Y,
            entry.DefaultBounds.Width,
            entry.DefaultBounds.Height);
        SceneObject sceneObject = typeId switch
        {
            "text" => TextObject.Create("文本标签", bounds),
            ControlTypeIds.StraightPipe => PipeObject.Create(
                new PointD(bounds.X, bounds.Y + bounds.Height / 2),
                new PointD(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2)),
            _ => ControlObject.Create(typeId, bounds)
        };
        Session.AddObject(sceneObject);
        Refresh();
        Focus();
    }

    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
        {
            Session?.ClearSelection();
            Refresh();
            Focus();
        }
    }

    private void OnObjectMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Session is null || sender is not FrameworkElement element || element.Tag is not Guid objectId)
        {
            return;
        }

        Session.SelectOnly(objectId);
        Refresh();
        Focus();
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Session is null)
        {
            return;
        }

        var direction = e.Key switch
        {
            Key.Left => GeometryNudgeDirection.Left,
            Key.Right => GeometryNudgeDirection.Right,
            Key.Up => GeometryNudgeDirection.Up,
            Key.Down => GeometryNudgeDirection.Down,
            _ => (GeometryNudgeDirection?)null
        };
        if (direction is null)
        {
            return;
        }

        var offset = direction.Value switch
        {
            GeometryNudgeDirection.Left => new Vector(-1, 0),
            GeometryNudgeDirection.Right => new Vector(1, 0),
            GeometryNudgeDirection.Up => new Vector(0, -1),
            _ => new Vector(0, 1)
        };
        Session.MoveSelection(offset.X, offset.Y);
        Refresh();
        e.Handled = true;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        var factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
        Viewport.SetZoomAt(Viewport.Zoom * factor, e.GetPosition(this));
        Refresh();
        e.Handled = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
        {
            return;
        }

        _isPanning = true;
        _panStart = e.GetPosition(this);
        _panOrigin = Viewport.Pan;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || e.MiddleButton != MouseButtonState.Pressed)
        {
            return;
        }

        var delta = e.GetPosition(this) - _panStart;
        Viewport.SetPan(_panOrigin + delta);
        Refresh();
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_isPanning)
        {
            return;
        }

        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }
}
