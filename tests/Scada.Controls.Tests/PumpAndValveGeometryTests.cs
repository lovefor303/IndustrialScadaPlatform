using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class PumpAndValveGeometryTests
{
    [Fact]
    public void PumpHasCasingMotorCouplingAndBoundaryPorts()
    {
        var plan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));

        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "pump.casing");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "pump.motor");
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "pump.coupling");
        Assert.Equal(0, plan.Anchors["suction"].X);
        Assert.Equal(plan.DesignSize.Width, plan.Anchors["discharge"].X);
        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "pump.shaft" && primitive.AnimationName == "pump.rotate");
    }

    [Fact]
    public void PumpStoppedStateDoesNotAnimateMechanicalParts()
    {
        var plan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped));

        Assert.DoesNotContain(plan.Primitives, primitive => primitive.AnimationName is not null);
        Assert.Equal(ControlState.Stopped, plan.State);
    }

    [Fact]
    public void ReducedMotionSuppressesPumpAndValveAnimation()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with { ReducedMotion = true };

        var pump = PumpAndValveGeometry.BuildPump(context);
        var valve = PumpAndValveGeometry.BuildValve(context);

        Assert.Empty(pump.ActiveAnimations);
        Assert.Empty(valve.ActiveAnimations);
        Assert.DoesNotContain(pump.Primitives, primitive => primitive.AnimationName is not null);
        Assert.DoesNotContain(valve.Primitives, primitive => primitive.AnimationName is not null);
    }

    [Fact]
    public void ValveTravelAnimationTargetsStemNotEntireBody()
    {
        var plan = PumpAndValveGeometry.BuildValve(ControlRenderContext.ForState(ControlState.Transition));

        Assert.Contains(plan.Primitives, primitive => primitive.PartId == "valve.stem" && primitive.AnimationName == "valve.travel");
        Assert.DoesNotContain(plan.Primitives, primitive => primitive.PartId == "valve.body" && primitive.AnimationName is not null);
        Assert.Equal(0, plan.Anchors["inlet"].X);
        Assert.Equal(plan.DesignSize.Width, plan.Anchors["outlet"].X);
    }

    [Fact]
    public void ValvePositionIsClampedAndDoesNotMovePipeAnchors()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["ValvePosition"] = 150 }
        };

        var plan = PumpAndValveGeometry.BuildValve(context);

        Assert.Equal("100", plan.Diagnostics["valve.position"]);
        Assert.Equal(0, plan.Anchors["inlet"].X);
        Assert.Equal(plan.DesignSize.Width, plan.Anchors["outlet"].X);
    }
}
