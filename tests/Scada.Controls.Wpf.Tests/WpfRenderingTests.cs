using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IO = System.IO;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Preview.Wpf;
using Scada.Core;
using Xunit;

namespace Scada.Controls.Wpf.Tests;

public sealed class WpfRenderingTests
{
    [Fact]
    public Task PumpPlanCreatesNamedNativeWpfParts() => StaThread.RunAsync(() =>
    {
        var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));
        var visual = WpfControlRenderer.Render(pumpPlan);

        Assert.NotNull(VisualSearch.ByPartId(visual, "pump.casing"));
        Assert.NotNull(VisualSearch.ByPartId(visual, "pump.motor"));
    });

    [Fact]
    public Task ResizeScalesBodyAndNotOnlySelectionBounds() => StaThread.RunAsync(() =>
    {
        var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped));
        var control = new IndustrialControl { RenderPlan = pumpPlan, Width = 240, Height = 144 };

        control.Measure(new Size(240, 144));
        control.Arrange(new Rect(0, 0, 240, 144));

        Assert.Equal(2d, control.GeometryScaleX, precision: 3);
        Assert.Equal(2d, control.GeometryScaleY, precision: 3);
    });

    [Fact]
    public Task RendererMapsTextAndAssignsStablePartId() => StaThread.RunAsync(() =>
    {
        var plan = new ControlRenderPlan(
            "test.label", 1, new RenderSize(100, 40),
            [new RenderText("label", new RenderPoint(12, 18), "P-101", VisualTokens.EquipmentBody)],
            new Dictionary<string, RenderPoint>(), ControlState.Neutral,
            new HashSet<string>(), new Dictionary<string, string>());

        var visual = WpfControlRenderer.Render(plan);

        Assert.NotNull(VisualSearch.ByPartId(visual, "label"));
    });

    [Fact]
    public Task RendererMapsAllPrimitiveFamiliesAndMarksUnknownQuality() => StaThread.RunAsync(() =>
    {
        var plan = new ControlRenderPlan(
            "test.primitives", 1, new RenderSize(100, 60),
            [
                new RenderLine("line", new(0, 0), new(10, 0), VisualTokens.Outline),
                new RenderRectangle("rectangle", new(1, 1, 6, 6), VisualTokens.EquipmentBody),
                new RenderEllipse("ellipse", new(2, 2, 6, 6), VisualTokens.EquipmentBody),
                new RenderPath("path", [new MoveTo(new(0, 8)), new LineTo(new(8, 8))], VisualTokens.Outline),
                new RenderGroup("group", [new RenderText("text", new(3, 12), "M", VisualTokens.EquipmentBody)])
            ],
            new Dictionary<string, RenderPoint>(), ControlState.Unknown,
            new HashSet<string>(), new Dictionary<string, string>());

        var visual = WpfControlRenderer.Render(plan);

        Assert.IsType<Line>(VisualSearch.ByPartId(visual, "line"));
        Assert.IsType<Rectangle>(VisualSearch.ByPartId(visual, "rectangle"));
        Assert.IsType<Ellipse>(VisualSearch.ByPartId(visual, "ellipse"));
        Assert.IsType<Path>(VisualSearch.ByPartId(visual, "path"));
        Assert.IsType<TextBlock>(VisualSearch.ByPartId(visual, "text"));
        Assert.NotNull(VisualSearch.ByPartId(visual, "control.quality-unknown"));
    });

    [Fact]
    public Task StateOverrideChangesRenderedSemanticStateAndStopsActiveAnimation() => StaThread.RunAsync(() =>
    {
        var activePump = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));

        var visual = WpfControlRenderer.Render(activePump, ControlState.Unknown, reducedMotion: false);
        var impeller = Assert.IsType<Ellipse>(VisualSearch.ByPartId(visual, "pump.impeller"));

        Assert.Equal(ControlState.Unknown, WpfControlRenderer.GetControlState(visual));
        Assert.Equal(VariableQuality.Bad, WpfControlRenderer.GetQuality(visual));
        Assert.NotNull(VisualSearch.ByPartId(visual, "control.quality-unknown"));
        Assert.IsNotType<RotateTransform>(impeller.RenderTransform);
    });

    [Fact]
    public Task BadQualityPlanStopsActiveAnimationEvenWhenPlanStateIsActive() => StaThread.RunAsync(() =>
    {
        var activePump = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));
        var badPlan = new ControlRenderPlan(
            activePump.TypeId,
            activePump.Version,
            activePump.DesignSize,
            [.. activePump.Primitives, new RenderEllipse("quality.unknown", new(108, 2, 8, 8), VisualTokens.Unknown)],
            activePump.Anchors,
            ControlState.Active,
            activePump.ActiveAnimations,
            activePump.Diagnostics);

        var visual = WpfControlRenderer.Render(badPlan);
        var impeller = Assert.IsType<Ellipse>(VisualSearch.ByPartId(visual, "pump.impeller"));

        Assert.Equal(VariableQuality.Bad, WpfControlRenderer.GetQuality(visual));
        Assert.IsNotType<RotateTransform>(impeller.RenderTransform);
    });

    [Fact]
    public Task ReducedMotionStopsActiveAnimation() => StaThread.RunAsync(() =>
    {
        var activePump = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));

        var visual = WpfControlRenderer.Render(activePump, stateOverride: null, reducedMotion: true);
        var impeller = Assert.IsType<Ellipse>(VisualSearch.ByPartId(visual, "pump.impeller"));

        Assert.IsNotType<RotateTransform>(impeller.RenderTransform);
    });

    [Theory]
    [InlineData(ControlOrientation.Normal)]
    [InlineData(ControlOrientation.Right)]
    [InlineData(ControlOrientation.UpsideDown)]
    [InlineData(ControlOrientation.Left)]
    public Task EveryOrientationFitsItsAllocatedBounds(ControlOrientation orientation) => StaThread.RunAsync(() =>
    {
        var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped));
        var control = new IndustrialControl
        {
            RenderPlan = pumpPlan,
            Orientation = orientation,
            Width = 240,
            Height = 144
        };

        control.Measure(new Size(240, 144));
        control.Arrange(new Rect(0, 0, 240, 144));

        Assert.InRange(control.RenderedGeometryBounds.Left, 0d, 240d);
        Assert.InRange(control.RenderedGeometryBounds.Top, 0d, 144d);
        Assert.InRange(control.RenderedGeometryBounds.Right, 0d, 240d);
        Assert.InRange(control.RenderedGeometryBounds.Bottom, 0d, 144d);
    });

    [Theory]
    [InlineData(ControlOrientation.Normal)]
    [InlineData(ControlOrientation.Right)]
    [InlineData(ControlOrientation.UpsideDown)]
    [InlineData(ControlOrientation.Left)]
    public Task ActualRenderedGeometryFitsAllocatedBoundsForEveryOrientation(ControlOrientation orientation) => StaThread.RunAsync(() =>
    {
        var control = new IndustrialControl
        {
            RenderPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped)),
            Orientation = orientation,
            Width = 240,
            Height = 144
        };
        var host = new Border { Width = 240, Height = 144, Child = control };
        host.Measure(new Size(240, 144));
        host.Arrange(new Rect(0, 0, 240, 144));
        host.UpdateLayout();

        var visual = Assert.IsType<Canvas>(VisualSearch.ByPartId(control, "control.root"));
        var bounds = visual.TransformToAncestor(control).TransformBounds(new Rect(0, 0, 120, 72));

        Assert.InRange(bounds.Left, -0.01d, 240d);
        Assert.InRange(bounds.Top, -0.01d, 144d);
        Assert.InRange(bounds.Right, 0d, 240.01d);
        Assert.InRange(bounds.Bottom, 0d, 144.01d);
    });

    [Fact]
    public Task DynamicThemeResourceFromHostOverridesGenericToken() => StaThread.RunAsync(() =>
    {
        var host = new Border();
        var overrideBrush = new SolidColorBrush(Colors.Magenta);
        host.Resources["ScadaBrush.#D4D9DC"] = overrideBrush;
        var visual = WpfControlRenderer.Render(PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped)));
        host.Child = visual;
        host.Measure(new Size(120, 72));
        host.Arrange(new Rect(0, 0, 120, 72));
        host.UpdateLayout();

        var motor = Assert.IsType<Rectangle>(VisualSearch.ByPartId(visual, "pump.motor"));

        Assert.Same(overrideBrush, motor.Fill);
    });

    [Fact]
    public Task GenericThemeProvidesDefaultBrushesForRenderedControlParts() => StaThread.RunAsync(() =>
    {
        var control = new IndustrialControl
        {
            RenderPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped)),
            Width = 120,
            Height = 72
        };
        var host = new Border { Width = 120, Height = 72, Child = control };
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/Scada.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative)
        });
        host.Measure(new Size(120, 72));
        host.Arrange(new Rect(0, 0, 120, 72));
        host.UpdateLayout();

        var motor = Assert.IsType<Rectangle>(VisualSearch.ByPartId(control, "pump.motor"));

        Assert.NotNull(motor.Fill);
        Assert.IsType<SolidColorBrush>(motor.Fill);
    });

    [Fact]
    public Task ReviewPngsCoverEveryControlTypeWithoutDimensionClipping() => StaThread.RunAsync(() =>
    {
        var outputDirectory = IO.Path.Combine(IO.Path.GetTempPath(), $"scada-wpf-review-{Guid.NewGuid():N}");
        try
        {
            var result = ReviewArtifactWriter.Write(outputDirectory);

            Assert.Equal(ControlTypeIds.All.OrderBy(typeId => typeId, StringComparer.Ordinal), result.RenderedControlTypeIds);
            AssertPngDimensions(result.ArtifactPaths["magnified-active"], 3072, 1728);
            AssertPngDimensions(result.ArtifactPaths["magnified-fault"], 3072, 1728);
            AssertPngDimensions(result.ArtifactPaths["exact-size"], 1920, 1080);
            AssertExtentFits(result.ContentExtents["magnified-active"], 3072, 1728);
            AssertExtentFits(result.ContentExtents["magnified-fault"], 3072, 1728);
            AssertExtentFits(result.ContentExtents["exact-size"], 1920, 1080);
        }
        finally
        {
            if (IO.Directory.Exists(outputDirectory))
            {
                IO.Directory.Delete(outputDirectory, recursive: true);
            }
        }
    });

    private static void AssertPngDimensions(string path, int expectedWidth, int expectedHeight)
    {
        Assert.True(IO.File.Exists(path));
        using var stream = IO.File.OpenRead(path);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.Equal(expectedWidth, frame.PixelWidth);
        Assert.Equal(expectedHeight, frame.PixelHeight);
    }

    private static void AssertExtentFits(ReviewArtifactExtent extent, int width, int height)
    {
        Assert.InRange(extent.Width, 0d, width);
        Assert.InRange(extent.Height, 0d, height);
    }
}
