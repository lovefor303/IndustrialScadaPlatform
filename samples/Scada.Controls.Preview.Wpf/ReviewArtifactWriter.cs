using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Scada.Controls.SampleProject;
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
        WriteSheet(Path.Combine(absoluteDirectory, "magnified-active.png"), 1440, 960, "active", scale: 1.6);
        WriteSheet(Path.Combine(absoluteDirectory, "magnified-fault.png"), 1440, 960, "fault", scale: 1.6);
        WriteSheet(Path.Combine(absoluteDirectory, "exact-size-1920x1080.png"), 1920, 1080, "stopped", scale: 1);
    }

    private static void WriteSheet(string path, int width, int height, string scenarioName, double scale)
    {
        var surface = new Canvas
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(32, 36, 40))
        };
        foreach (var renderable in SampleProjectFactory.BuildRenderables(scenarioName, reducedMotion: true))
        {
            var control = new IndustrialControl
            {
                RenderPlan = renderable.Plan,
                State = renderable.Plan.State,
                ReducedMotion = true,
                Width = renderable.Control.Bounds.Width * scale,
                Height = renderable.Control.Bounds.Height * scale
            };
            Canvas.SetLeft(control, renderable.Control.Bounds.X * scale);
            Canvas.SetTop(control, renderable.Control.Bounds.Y * scale);
            surface.Children.Add(control);
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
