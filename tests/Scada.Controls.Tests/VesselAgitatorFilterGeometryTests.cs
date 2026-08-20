using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Core;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class VesselAgitatorFilterGeometryTests
{
    [Fact]
    public void VesselExposesShellJacketLiquidAndBottomOutletAsDistinctParts()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["LevelValue"] = 62 }
        };

        var plan = VesselAgitatorFilterGeometry.BuildVessel(context);

        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "vessel.shell");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "vessel.jacket");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "vessel.liquid");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "vessel.bottom-outlet");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "vessel.top-inlet");
        Assert.Equal(0, plan.Anchors["top-inlet"].Y);
        Assert.Equal(plan.DesignSize.Height, plan.Anchors["bottom-outlet"].Y);
        Assert.Equal("62", plan.Diagnostics["vessel.level"]);
    }

    [Fact]
    public void VesselClampsLevelAndPreservesVisibleHeadspaceAtFullScale()
    {
        var plan = VesselAgitatorFilterGeometry.BuildVessel(ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["LevelValue"] = 150 }
        });

        var liquid = Assert.IsType<RenderRectangle>(plan.Primitives.Single(primitive => primitive.PartId == "vessel.liquid"));
        var shell = Assert.IsType<RenderPath>(plan.Primitives.Single(primitive => primitive.PartId == "vessel.shell"));

        Assert.Equal("100", plan.Diagnostics["vessel.level"]);
        Assert.True(liquid.Bounds.Y > 20, "Full liquid must leave visible empty headspace.");
        Assert.NotEmpty(shell.Commands);
    }

    [Fact]
    public void VesselHasNoResidualLiquidFillBelowZeroPercent()
    {
        var plan = VesselAgitatorFilterGeometry.BuildVessel(ControlRenderContext.ForState(ControlState.Stopped) with
        {
            NumericValues = new Dictionary<string, double> { ["LevelValue"] = -25 }
        });

        var liquid = Assert.IsType<RenderRectangle>(plan.Primitives.Single(primitive => primitive.PartId == "vessel.liquid"));

        Assert.Equal("0", plan.Diagnostics["vessel.level"]);
        Assert.Equal(0, liquid.Bounds.Height);
    }

    [Fact]
    public void AgitatorRotatesOnlyShaftAndImpellerWhenActive()
    {
        var plan = VesselAgitatorFilterGeometry.BuildAgitator(
            ControlRenderContext.ForState(ControlState.Active));

        Assert.Equal("agitator.rotate", plan.Primitives.Single(primitive => primitive.PartId == "agitator.shaft").AnimationName);
        Assert.Equal("agitator.rotate", plan.Primitives.Single(primitive => primitive.PartId == "agitator.impeller").AnimationName);
        Assert.Null(plan.Primitives.Single(primitive => primitive.PartId == "agitator.motor").AnimationName);
        Assert.DoesNotContain(plan.Primitives, primitive => primitive.PartId.StartsWith("vessel.", StringComparison.Ordinal)
            || primitive.PartId == "agitator.vessel");
        Assert.Equal(["agitator.rotate"], plan.ActiveAnimations);
    }

    [Fact]
    public void AgitatorStopsAnimationWhenFeedbackQualityIsBad()
    {
        var plan = VesselAgitatorFilterGeometry.BuildAgitator(ControlRenderContext.ForState(ControlState.Active) with
        {
            Quality = VariableQuality.Bad
        });

        Assert.Empty(plan.ActiveAnimations);
        Assert.DoesNotContain(plan.Primitives, primitive => primitive.AnimationName is not null);
    }

    [Fact]
    public void FilterKeepsBoundaryPortsWhenFaultMarksHousingBlocked()
    {
        var stopped = VesselAgitatorFilterGeometry.BuildFilter(ControlRenderContext.ForState(ControlState.Stopped));
        var blocked = VesselAgitatorFilterGeometry.BuildFilter(ControlRenderContext.ForState(ControlState.Fault));

        Assert.Contains(blocked.Primitives, primitive => primitive.PartId == "filter.housing");
        Assert.Contains(blocked.Primitives, primitive => primitive.PartId == "filter.blocked-indicator" && primitive.Token == VisualTokens.Fault);
        Assert.Equal("blocked", blocked.Diagnostics["filter.condition"]);
        Assert.Equal(100, blocked.DesignSize.Width);
        Assert.Equal(160, blocked.DesignSize.Height);
        Assert.Equal(stopped.Anchors["inlet"], blocked.Anchors["inlet"]);
        Assert.Equal(stopped.Anchors["outlet"], blocked.Anchors["outlet"]);
        Assert.Equal(0, blocked.Anchors["inlet"].X);
        Assert.Equal(blocked.DesignSize.Width, blocked.Anchors["outlet"].X);
    }
}
