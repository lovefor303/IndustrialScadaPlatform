using System.IO;
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
    private const double RotationDegreesPerPixel = 0.01;
    private static readonly Cursor RotationCursor = RotationCursorFactory.Create();
    private bool _isPanning;
    private Point _panStart;
    private Vector _panOrigin;
    private bool _isSelecting;
    private bool _selectionMoved;
    private Point _selectionStart;
    private Rectangle? _selectionRectangle;
    private EditorSession? _session;
    private bool _suppressProjectRefresh;
    private bool _isDraggingObject;
    private Point _objectDragLast;

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

    public EditorSession? Session
    {
        get => _session;
        set
        {
            if (ReferenceEquals(_session, value))
            {
                return;
            }

            if (_session is not null)
            {
                _session.ProjectChanged -= OnSessionProjectChanged;
            }

            _session = value;
            if (_session is not null)
            {
                _session.ProjectChanged += OnSessionProjectChanged;
            }
        }
    }

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

    private void OnSessionProjectChanged(object? sender, EventArgs e)
    {
        if (!_suppressProjectRefresh)
        {
            Refresh();
        }
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
            element.Cursor = Cursors.SizeAll;
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
            Cursor = Cursors.SizeAll,
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
                Cursor = ResizeCursor(position),
                Tag = new ResizeHandleTag(sceneObject.Id, position)
            };
            handle.DragDelta += OnResizeHandleDragDelta;
            handle.DragCompleted += OnGeometryHandleDragCompleted;
            Children.Add(handle);
            SetHandlePosition(handle, bounds, position);
        }

        var rotationHandle = new Thumb
        {
            Width = 8,
            Height = 8,
            Background = Brushes.White,
            BorderBrush = Brushes.DeepSkyBlue,
            BorderThickness = new Thickness(1),
            Cursor = RotationCursor,
            Tag = "rotation"
        };
        rotationHandle.DragDelta += (_, args) => OnRotationHandleDragDelta(sceneObject.Id, args);
        rotationHandle.DragCompleted += OnGeometryHandleDragCompleted;
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
        _suppressProjectRefresh = true;
        try
        {
            Session.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Bounds = ResizeBounds(sceneObject.Bounds, tag.Position, dx, dy)
            });
        }
        finally
        {
            _suppressProjectRefresh = false;
        }
        UpdateObjectVisual(tag.ObjectId);
    }

    private void OnRotationHandleDragDelta(Guid objectId, DragDeltaEventArgs e)
    {
        if (Session is null)
        {
            return;
        }

        Session.SelectOnly(objectId);
        _suppressProjectRefresh = true;
        try
        {
            Session.UpdateSelectedObject(selected => selected with
            {
                Rotation = selected.Rotation + e.HorizontalChange * RotationDegreesPerPixel
            });
        }
        finally
        {
            _suppressProjectRefresh = false;
        }
        UpdateObjectVisual(objectId);
    }

    private void OnGeometryHandleDragCompleted(object sender, DragCompletedEventArgs e)
    {
        Refresh();
    }

    private void UpdateObjectVisual(Guid objectId)
    {
        if (Session is null || Session.ActiveScreen.FindObject(objectId) is not { } sceneObject)
        {
            return;
        }

        var visual = Children.OfType<FrameworkElement>()
            .FirstOrDefault(element => element.Tag is Guid id && id == objectId);
        if (visual is not null)
        {
            ApplyVisualGeometry(visual, sceneObject);
        }

        foreach (var handle in Children.OfType<Thumb>())
        {
            if (handle.Tag is ResizeHandleTag resize && resize.ObjectId == objectId)
            {
                SetHandlePosition(handle, sceneObject.Bounds, resize.Position);
            }
            else if (Equals(handle.Tag, "rotation"))
            {
                SetRotationHandlePosition(handle, sceneObject.Bounds);
            }
        }

        InvalidateVisual();
    }

    private void SetRotationHandlePosition(Thumb handle, RectD bounds)
    {
        SetLeft(handle, (bounds.X + bounds.Width / 2) * Viewport.Zoom + Viewport.Pan.X - 4);
        SetTop(handle, (bounds.Y - 20) * Viewport.Zoom + Viewport.Pan.Y);
    }

    private void ApplyVisualGeometry(FrameworkElement visual, SceneObject sceneObject)
    {
        var width = sceneObject.Bounds.Width * Viewport.Zoom;
        var height = sceneObject.Bounds.Height * Viewport.Zoom;
        visual.Width = width;
        visual.Height = height;
        SetLeft(visual, sceneObject.Bounds.X * Viewport.Zoom + Viewport.Pan.X);
        SetTop(visual, sceneObject.Bounds.Y * Viewport.Zoom + Viewport.Pan.Y);

        if (visual is Border { Child: FrameworkElement child })
        {
            visual.RenderTransform = null;
            child.Width = width;
            child.Height = height;
            child.RenderTransform = new RotateTransform(sceneObject.Rotation);
            child.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            visual.RenderTransform = new RotateTransform(sceneObject.Rotation);
            visual.RenderTransformOrigin = new Point(0.5, 0.5);
        }
    }

    private static Cursor ResizeCursor(HandlePosition position) => position switch
    {
        HandlePosition.TopLeft or HandlePosition.BottomRight => Cursors.SizeNWSE,
        HandlePosition.TopRight or HandlePosition.BottomLeft => Cursors.SizeNESW,
        HandlePosition.Top or HandlePosition.Bottom => Cursors.SizeNS,
        HandlePosition.Left or HandlePosition.Right => Cursors.SizeWE,
        _ => Cursors.SizeAll
    };

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
            _isSelecting = true;
            _selectionMoved = false;
            _selectionStart = e.GetPosition(this);
            _selectionRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(48, 30, 144, 255)),
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Children.Add(_selectionRectangle);
            SetLeft(_selectionRectangle, _selectionStart.X);
            SetTop(_selectionRectangle, _selectionStart.Y);
            CaptureMouse();
            Focus();
            e.Handled = true;
        }
    }

    private void OnObjectMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Session is null || sender is not FrameworkElement element || element.Tag is not Guid objectId)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Session.ToggleSelection(objectId);
            Refresh();
            Focus();
            e.Handled = true;
            return;
        }

        if (!Session.SelectedObjectIds.Contains(objectId))
        {
            Session.SelectOnly(objectId);
            Refresh();
        }

        _isDraggingObject = true;
        _objectDragLast = e.GetPosition(this);
        CaptureMouse();
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
        if (_isDraggingObject && e.LeftButton == MouseButtonState.Pressed && Session is not null)
        {
            var current = e.GetPosition(this);
            var objectDelta = current - _objectDragLast;
            if (objectDelta.LengthSquared > 0)
            {
                _suppressProjectRefresh = true;
                try
                {
                    Session.MoveSelection(objectDelta.X / Viewport.Zoom, objectDelta.Y / Viewport.Zoom);
                }
                finally
                {
                    _suppressProjectRefresh = false;
                }

                Refresh();
                _objectDragLast = current;
            }

            e.Handled = true;
            return;
        }

        if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(this);
            var width = Math.Abs(current.X - _selectionStart.X);
            var height = Math.Abs(current.Y - _selectionStart.Y);
            _selectionMoved = width >= 3 || height >= 3;
            if (_selectionRectangle is not null)
            {
                SetLeft(_selectionRectangle, Math.Min(_selectionStart.X, current.X));
                SetTop(_selectionRectangle, Math.Min(_selectionStart.Y, current.Y));
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;
            }

            e.Handled = true;
            return;
        }

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
        if (e.ChangedButton == MouseButton.Left && _isDraggingObject)
        {
            _isDraggingObject = false;
            ReleaseMouseCapture();
            Refresh();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _isSelecting)
        {
            var end = e.GetPosition(this);
            ReleaseMouseCapture();
            _isSelecting = false;
            if (_selectionRectangle is not null)
            {
                Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
            }

            if (_selectionMoved && Session is not null)
            {
                var startModel = Viewport.ScreenToModel(_selectionStart);
                var endModel = Viewport.ScreenToModel(end);
                Session.SelectIntersecting(new RectD(
                    startModel.X,
                    startModel.Y,
                    endModel.X - startModel.X,
                    endModel.Y - startModel.Y));
                Refresh();
            }
            else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Session?.ClearSelection();
                Refresh();
            }

            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Middle || !_isPanning)
        {
            return;
        }

        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }
}

internal static class RotationCursorFactory
{
    public static Cursor Create()
    {
        try
        {
            const int size = 32;
            const int pixelBytes = size * size * 4;
            const int maskBytes = size * (size / 8);
            const int imageOffset = 22;
            const int imageSize = 40 + pixelBytes + maskBytes;

            using var stream = new MemoryStream(imageOffset + imageSize);
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                // CUR header and one 32-bit DIB image. For a cursor, planes/bitcount
                // in the directory entry store the hotspot coordinates.
                writer.Write((ushort)0);
                writer.Write((ushort)2);
                writer.Write((ushort)1);
                writer.Write((byte)size);
                writer.Write((byte)size);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)(size / 2));
                writer.Write((ushort)(size / 2));
                writer.Write(imageSize);
                writer.Write(imageOffset);

                writer.Write(40);
                writer.Write(size);
                writer.Write(size * 2);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(0);
                writer.Write(pixelBytes);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);

                var pixels = new bool[size, size];
                DrawArc(pixels, 16, 16, 10.5, 35, 315);
                DrawArrow(pixels, 24, 8, 22, 4, 25, 12);
                DrawArrow(pixels, 8, 24, 4, 25, 12, 22);

                for (var y = size - 1; y >= 0; y--)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var on = pixels[x, y];
                        writer.Write(on ? (byte)255 : (byte)0);
                        writer.Write(on ? (byte)191 : (byte)0);
                        writer.Write(on ? (byte)0 : (byte)0);
                        writer.Write(on ? (byte)255 : (byte)0);
                    }
                }

                for (var i = 0; i < maskBytes; i++)
                {
                    writer.Write((byte)0);
                }
            }

            stream.Position = 0;
            return new Cursor(stream);
        }
        catch (Exception)
        {
            // Keep the editor usable on WPF environments that reject custom DIB
            // cursors; ScrollAll still communicates a rotation gesture better
            // than a generic hand cursor.
            return Cursors.ScrollAll;
        }
    }

    private static void DrawArc(bool[,] pixels, double centerX, double centerY, double radius, double start, double end)
    {
        for (var angle = start; angle <= end; angle += 2)
        {
            var radians = angle * Math.PI / 180;
            var x = (int)Math.Round(centerX + radius * Math.Cos(radians));
            var y = (int)Math.Round(centerY + radius * Math.Sin(radians));
            SetPixel(pixels, x, y);
            SetPixel(pixels, x + 1, y);
            SetPixel(pixels, x, y + 1);
        }
    }

    private static void DrawArrow(bool[,] pixels, int tipX, int tipY, int leftX, int leftY, int rightX, int rightY)
    {
        DrawLine(pixels, tipX, tipY, leftX, leftY);
        DrawLine(pixels, tipX, tipY, rightX, rightY);
    }

    private static void DrawLine(bool[,] pixels, int x1, int y1, int x2, int y2)
    {
        var steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        for (var i = 0; i <= steps; i++)
        {
            var fraction = steps == 0 ? 0 : (double)i / steps;
            SetPixel(pixels,
                (int)Math.Round(x1 + (x2 - x1) * fraction),
                (int)Math.Round(y1 + (y2 - y1) * fraction));
        }
    }

    private static void SetPixel(bool[,] pixels, int x, int y)
    {
        if (x >= 0 && x < pixels.GetLength(0) && y >= 0 && y < pixels.GetLength(1))
        {
            pixels[x, y] = true;
        }
    }
}
