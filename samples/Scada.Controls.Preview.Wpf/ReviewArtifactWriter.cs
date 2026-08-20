using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Wpf;

namespace Scada.Controls.Preview.Wpf;

internal static class ReviewArtifactWriter
{
    public static void Write(string absoluteDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteDirectory);
        if (!Path.IsPathFullyQualified(absoluteDirectory))
        {
            throw new ArgumentException("Review artifact directory must be absolute.", nameof(absoluteDirectory));
        }

        Directory.CreateDirectory(absoluteDirectory);
        WriteSheet(Path.Combine(absoluteDirectory, "magnified-active.png"), 1440, 960, ControlState.Active, scale: 2.4);
        WriteSheet(Path.Combine(absoluteDirectory, "magnified-fault.png"), 1440, 960, ControlState.Fault, scale: 2.4);
        WriteSheet(Path.Combine(absoluteDirectory, "exact-size-1920x1080.png"), 1920, 1080, ControlState.Stopped, scale: 1);
    }

    private static void WriteSheet(string path, int width, int height, ControlState state, double scale)
    {
        var surface = new Canvas
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(32, 36, 40))
        };
        var context = PreviewPlans.CreateContext(state, reducedMotion: true);
        var x = 60d;
        var y = 55d;
        foreach (var typeId in ControlTypeIds.All)
        {
            var plan = ControlGeometryFactory.Build(typeId, context);
            var control = new IndustrialControl
            {
                RenderPlan = plan,
                State = state,
                ReducedMotion = true,
                Width = Math.Min(360, Math.Max(120, plan.DesignSize.Width * scale)),
                Height = Math.Min(420, Math.Max(70, plan.DesignSize.Height * scale))
            };
            Canvas.SetLeft(control, x);
            Canvas.SetTop(control, y + 24);
            surface.Children.Add(new TextBlock
            {
                Text = typeId,
                Foreground = Brushes.White,
                FontSize = 13
            });
            Canvas.SetLeft(surface.Children[^1], x);
            Canvas.SetTop(surface.Children[^1], y);
            surface.Children.Add(control);
            x += 400;
            if (x > width - 380)
            {
                x = 60;
                y += 250;
            }
        }

        surface.Measure(new Size(width, height));
        surface.Arrange(new Rect(0, 0, width, height));
        surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
