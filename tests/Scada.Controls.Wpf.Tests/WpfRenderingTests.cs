using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
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
}
