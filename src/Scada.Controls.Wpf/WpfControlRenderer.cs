using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Scada.Controls.Rendering;

namespace Scada.Controls.Wpf;

/// <summary>
/// Converts the platform-neutral control render contract into native WPF visuals.
/// It never interprets XAML, so every rendered part retains its stable SDK identifier.
/// </summary>
public static class WpfControlRenderer
{
    public static readonly DependencyProperty PartIdProperty = DependencyProperty.RegisterAttached(
        "PartId", typeof(string), typeof(WpfControlRenderer), new FrameworkPropertyMetadata(string.Empty));

    public static string GetPartId(DependencyObject element) => (string)element.GetValue(PartIdProperty);

    public static void SetPartId(DependencyObject element, string value) => element.SetValue(PartIdProperty, value);

    public static Canvas Render(ControlRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var root = new Canvas
        {
            Width = plan.DesignSize.Width,
            Height = plan.DesignSize.Height,
            ClipToBounds = false,
            IsHitTestVisible = false
        };
        SetPartId(root, "control.root");

        foreach (var primitive in plan.Primitives)
        {
            root.Children.Add(CreateElement(primitive, plan));
        }

        if (plan.State == ControlState.Unknown)
        {
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = BrushFor(VisualTokens.Unknown),
                Stroke = BrushFor(VisualTokens.Outline),
                StrokeThickness = 1
            };
            SetPartId(marker, "control.quality-unknown");
            Canvas.SetLeft(marker, Math.Max(0, plan.DesignSize.Width - 10));
            Canvas.SetTop(marker, 2);
            root.Children.Add(marker);
        }

        return root;
    }

    private static UIElement CreateElement(RenderPrimitive primitive, ControlRenderPlan plan)
    {
        UIElement element = primitive switch
        {
            RenderLine line => CreateLine(line),
            RenderRectangle rectangle => CreateRectangle(rectangle),
            RenderEllipse ellipse => CreateEllipse(ellipse),
            RenderText text => CreateText(text),
            RenderPath path => CreatePath(path),
            RenderGroup group => CreateGroup(group, plan),
            _ => throw new NotSupportedException($"Unsupported render primitive '{primitive.GetType().Name}'.")
        };

        SetPartId(element, primitive.PartId);
        ApplyAnimation(element, primitive, plan);
        return element;
    }

    private static Line CreateLine(RenderLine line) => new()
    {
        X1 = line.Start.X,
        Y1 = line.Start.Y,
        X2 = line.End.X,
        Y2 = line.End.Y,
        Stroke = BrushFor(line.Token),
        StrokeThickness = StrokeFor(line.PartId)
    };

    private static Rectangle CreateRectangle(RenderRectangle rectangle)
    {
        var shape = new Rectangle
        {
            Width = rectangle.Bounds.Width,
            Height = rectangle.Bounds.Height,
            Fill = BrushFor(rectangle.Token),
            Stroke = BrushFor(VisualTokens.Outline),
            StrokeThickness = VisualTokens.EquipmentStroke
        };
        Canvas.SetLeft(shape, rectangle.Bounds.X);
        Canvas.SetTop(shape, rectangle.Bounds.Y);
        return shape;
    }

    private static Ellipse CreateEllipse(RenderEllipse ellipse)
    {
        var shape = new Ellipse
        {
            Width = ellipse.Bounds.Width,
            Height = ellipse.Bounds.Height,
            Fill = BrushFor(ellipse.Token),
            Stroke = BrushFor(VisualTokens.Outline),
            StrokeThickness = VisualTokens.EquipmentStroke
        };
        Canvas.SetLeft(shape, ellipse.Bounds.X);
        Canvas.SetTop(shape, ellipse.Bounds.Y);
        return shape;
    }

    private static TextBlock CreateText(RenderText text)
    {
        var block = new TextBlock
        {
            Text = text.Text,
            Foreground = BrushFor(text.Token),
            FontSize = 12,
            FontFamily = new FontFamily("Segoe UI"),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(block, text.Position.X);
        Canvas.SetTop(block, text.Position.Y);
        return block;
    }

    private static Path CreatePath(RenderPath path) => new()
    {
        Data = CreateGeometry(path.Commands),
        Fill = BrushFor(path.Token),
        Stroke = BrushFor(path.Token),
        StrokeThickness = StrokeFor(path.PartId),
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round
    };

    private static Canvas CreateGroup(RenderGroup group, ControlRenderPlan plan)
    {
        var canvas = new Canvas { ClipToBounds = false, IsHitTestVisible = false };
        foreach (var child in group.Children)
        {
            canvas.Children.Add(CreateElement(child, plan));
        }

        return canvas;
    }

    private static StreamGeometry CreateGeometry(IReadOnlyList<PathCommand> commands)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        for (var index = 0; index < commands.Count; index++)
        {
            switch (commands[index])
            {
                case MoveTo move:
                    var nextMove = commands.Skip(index + 1).ToList().FindIndex(command => command is MoveTo);
                    var endIndex = nextMove < 0 ? commands.Count : index + nextMove + 1;
                    var isClosed = commands.Skip(index + 1).Take(endIndex - index - 1).Any(command => command is ClosePath);
                    context.BeginFigure(new Point(move.Point.X, move.Point.Y), isFilled: isClosed, isClosed: isClosed);
                    break;
                case LineTo line:
                    context.LineTo(new Point(line.Point.X, line.Point.Y), isStroked: true, isSmoothJoin: false);
                    break;
                case CubicTo cubic:
                    context.BezierTo(
                        new Point(cubic.Control1.X, cubic.Control1.Y),
                        new Point(cubic.Control2.X, cubic.Control2.Y),
                        new Point(cubic.Point.X, cubic.Point.Y),
                        isStroked: true,
                        isSmoothJoin: false);
                    break;
                case ClosePath:
                    break;
                default:
                    throw new NotSupportedException($"Unsupported path command '{commands[index].GetType().Name}'.");
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private static void ApplyAnimation(UIElement element, RenderPrimitive primitive, ControlRenderPlan plan)
    {
        if (primitive.AnimationName is null
            || !plan.ActiveAnimations.Contains(primitive.AnimationName)
            || plan.State == ControlState.Unknown)
        {
            return;
        }

        var transform = new RotateTransform();
        element.RenderTransform = transform;
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        transform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(
            0,
            360,
            new Duration(VisualTokens.StandardMotion))
        {
            RepeatBehavior = RepeatBehavior.Forever
        });
    }

    private static Brush BrushFor(string token)
    {
        if (Application.Current?.TryFindResource(TokenKey(token)) is Brush resourceBrush)
        {
            return resourceBrush;
        }

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(token));
    }

    private static string TokenKey(string token) => $"ScadaBrush.{token}";

    private static double StrokeFor(string partId) => partId.StartsWith("pipe.", StringComparison.Ordinal)
        ? VisualTokens.PipeStroke
        : VisualTokens.EquipmentStroke;
}
