using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Scada.Controls.SampleProject;
using Scada.Controls.Wpf;

namespace Scada.Controls.Preview.Wpf;

public sealed record ReviewArtifactResult(
    IReadOnlyList<string> RenderedControlTypeIds,
    IReadOnlyDictionary<string, string> ArtifactPaths,
    IReadOnlyDictionary<string, ReviewArtifactExtent> ContentExtents);

public sealed record ReviewArtifactExtent(double Width, double Height);

public static class ReviewArtifactWriter
{
    public static ReviewArtifactResult Write(string absoluteDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteDirectory);
        if (!Path.IsPathFullyQualified(absoluteDirectory))
        {
            throw new ArgumentException("Review artifact directory must be absolute.", nameof(absoluteDirectory));
        }

        Directory.CreateDirectory(absoluteDirectory);
        var activePath = Path.Combine(absoluteDirectory, "magnified-active.png");
        var faultPath = Path.Combine(absoluteDirectory, "magnified-fault.png");
        var exactSizePath = Path.Combine(absoluteDirectory, "exact-size-1920x1080.png");
        var renderedControlTypeIds = SampleProjectFactory.BuildRenderables("stopped")
            .Select(renderable => renderable.Plan.TypeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(typeId => typeId, StringComparer.Ordinal)
            .ToArray();

        var activeExtent = WriteSheet(activePath, 3072, 1728, "active", scale: 1.6);
        var faultExtent = WriteSheet(faultPath, 3072, 1728, "fault", scale: 1.6);
        var exactSizeExtent = WriteSheet(exactSizePath, 1920, 1080, "stopped", scale: 1);
        return new ReviewArtifactResult(
            renderedControlTypeIds,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["magnified-active"] = activePath,
                ["magnified-fault"] = faultPath,
                ["exact-size"] = exactSizePath
            },
            new Dictionary<string, ReviewArtifactExtent>(StringComparer.Ordinal)
            {
                ["magnified-active"] = activeExtent,
                ["magnified-fault"] = faultExtent,
                ["exact-size"] = exactSizeExtent
            });
    }

    private static ReviewArtifactExtent WriteSheet(string path, int width, int height, string scenarioName, double scale)
    {
        var surface = new Canvas
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(32, 36, 40))
        };
        var extentWidth = 0d;
        var extentHeight = 0d;
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
            extentWidth = Math.Max(extentWidth, (renderable.Control.Bounds.X + renderable.Control.Bounds.Width) * scale);
            extentHeight = Math.Max(extentHeight, (renderable.Control.Bounds.Y + renderable.Control.Bounds.Height) * scale);
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
        return new ReviewArtifactExtent(extentWidth, extentHeight);
    }
}
