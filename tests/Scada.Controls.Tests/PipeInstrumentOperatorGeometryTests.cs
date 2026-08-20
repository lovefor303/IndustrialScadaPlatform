using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Core;
using Scada.Scene;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class PipeInstrumentOperatorGeometryTests
{
    [Fact]
    public void StraightPipeIsIndependentAndHasFlowMarkerOnlyWithGoodFeedback()
    {
        var active = PipeInstrumentOperatorGeometry.BuildStraightPipe(
            ControlRenderContext.ForState(ControlState.Active));
        var bad = PipeInstrumentOperatorGeometry.BuildStraightPipe(
            ControlRenderContext.ForState(ControlState.Active) with { Quality = VariableQuality.Bad });

        Assert.Equal(new RenderPoint(0, 10), active.Anchors["start"]);
        Assert.Equal(new RenderPoint(120, 10), active.Anchors["end"]);
        Assert.Contains(active.Primitives, primitive => primitive.PartId == "pipe.body");
        Assert.Contains(active.Primitives, primitive => primitive.PartId == "pipe.flow-marker");
        Assert.Empty(bad.ActiveAnimations);
        Assert.DoesNotContain(bad.Primitives, primitive => primitive.PartId == "pipe.flow-marker");
    }

    [Fact]
    public void ElbowAndTeeKeepExplicitFittingGeometry()
    {
        var elbow = PipeInstrumentOperatorGeometry.BuildElbow(ControlRenderContext.ForState(ControlState.Stopped));
        var tee = PipeInstrumentOperatorGeometry.BuildTee(ControlRenderContext.ForState(ControlState.Stopped));

        Assert.Contains(elbow.Primitives, primitive => primitive.PartId == "pipe.elbow-path");
        Assert.Equal(new RenderPoint(0, 30), elbow.Anchors["inlet"]);
        Assert.Equal(new RenderPoint(30, 60), elbow.Anchors["outlet"]);
        Assert.Contains(tee.Primitives, primitive => primitive.PartId == "pipe.tee-path");
        Assert.Equal(new RenderPoint(0, 40), tee.Anchors["inlet"]);
        Assert.Equal(new RenderPoint(80, 40), tee.Anchors["outlet"]);
        Assert.Equal(new RenderPoint(40, 0), tee.Anchors["branch"]);
    }

    [Fact]
    public void LevelBarClampsVisualFillButRetainsRawValueAndQualityMarker()
    {
        var high = PipeInstrumentOperatorGeometry.BuildLevelBar(ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["ProcessValue"] = 125 }
        });
        var bad = PipeInstrumentOperatorGeometry.BuildLevelBar(ControlRenderContext.ForState(ControlState.Unknown) with
        {
            Quality = VariableQuality.Bad,
            NumericValues = new Dictionary<string, double> { ["ProcessValue"] = 50 }
        });

        Assert.Equal("125", high.Diagnostics["raw.value"]);
        Assert.Equal("100", high.Diagnostics["display.percent"]);
        Assert.Contains(high.Primitives, primitive => primitive.PartId == "level.fill");
        Assert.Contains(bad.Primitives, primitive => primitive.PartId == "quality.unknown");
    }

    [Fact]
    public void NumericIndicatorsRenderValueAndUnitTextWithStableFamilyIdentity()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["ProcessValue"] = 21.5 },
            TextValues = new Dictionary<string, string> { ["Unit"] = "°C" }
        };

        var temperature = PipeInstrumentOperatorGeometry.BuildTemperatureIndicator(context);
        var value = Assert.IsType<RenderText>(temperature.Primitives.Single(primitive => primitive.PartId == "instrument.value"));
        var unit = Assert.IsType<RenderText>(temperature.Primitives.Single(primitive => primitive.PartId == "instrument.unit"));

        Assert.Equal("21.5", value.Text);
        Assert.Equal("°C", unit.Text);
        Assert.Equal(ControlTypeIds.TemperatureIndicator, temperature.TypeId);
        Assert.Equal("temperature", temperature.Diagnostics["instrument.family"]);
    }

    [Fact]
    public void NumericDisplayClampsVisibleValueAndKeepsRawDiagnostic()
    {
        var plan = PipeInstrumentOperatorGeometry.BuildNumericDisplay(ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double>
            {
                ["ProcessValue"] = 125,
                ["Minimum"] = 0,
                ["Maximum"] = 100
            }
        });

        var value = Assert.IsType<RenderText>(plan.Primitives.Single(primitive => primitive.PartId == "instrument.value"));
        Assert.Equal("100", value.Text);
        Assert.Equal("125", plan.Diagnostics["raw.value"]);
        Assert.Equal("100", plan.Diagnostics["display.value"]);
    }

    [Theory]
    [InlineData(ControlTypeIds.TemperatureIndicator, "temperature")]
    [InlineData(ControlTypeIds.PressureIndicator, "pressure")]
    [InlineData(ControlTypeIds.FlowIndicator, "flow")]
    public void MeasurementFamiliesKeepTheirOwnIdentity(string typeId, string family)
    {
        var plan = ControlGeometryFactory.Build(typeId, ControlRenderContext.ForState(ControlState.Stopped));

        Assert.Equal(typeId, plan.TypeId);
        Assert.Equal(family, plan.Diagnostics["instrument.family"]);
    }

    [Fact]
    public void CommandButtonDescribesActionWithoutConfirmingFeedback()
    {
        var context = ControlRenderContext.ForState(ControlState.Transition) with
        {
            TextValues = new Dictionary<string, string>
            {
                ["Label"] = "启动泵",
                ["ActionKind"] = "write-command",
                ["ActionTarget"] = "Pump.StartCommand"
            }
        };

        var plan = PipeInstrumentOperatorGeometry.BuildCommandButton(context);
        var label = Assert.IsType<RenderText>(plan.Primitives.Single(primitive => primitive.PartId == "button.label"));

        Assert.Equal("启动泵", label.Text);
        Assert.Equal("write-command", plan.Diagnostics["action.kind"]);
        Assert.Equal("Pump.StartCommand", plan.Diagnostics["action.target"]);
        Assert.Equal("pending", plan.Diagnostics["action.state"]);
        Assert.DoesNotContain(plan.Primitives, primitive => primitive.PartId == "feedback.active");
    }

    [Fact]
    public void ConfirmedActiveStateDoesNotInventPendingCommandIntent()
    {
        var plan = PipeInstrumentOperatorGeometry.BuildCommandButton(
            ControlRenderContext.ForState(ControlState.Active));

        Assert.Equal("idle", plan.Diagnostics["action.state"]);
    }

    [Fact]
    public void GeometryFactoryDispatchesCatalogTypesAndRejectsUnknownIds()
    {
        var context = ControlRenderContext.ForState(ControlState.Stopped);

        foreach (var typeId in ControlTypeIds.All)
        {
            var plan = ControlGeometryFactory.Build(typeId, context);
            Assert.Equal(typeId, plan.TypeId);
        }

        var exception = Assert.Throws<NotSupportedException>(() => ControlGeometryFactory.Build("unknown.control", context));
        Assert.Equal("Control geometry type 'unknown.control' is not supported.", exception.Message);
    }

    [Fact]
    public void PipeEndpointHelpersPreserveSceneMetadataAndRecomputeBounds()
    {
        var pipe = PipeObject.Create(new PointD(10, 20), new PointD(30, 40), [new PointD(15, 80)]) with
        {
            Properties = new Dictionary<string, string> { ["color"] = "blue" },
            Bindings = new Dictionary<string, BindingDefinition> { ["flow"] = new("Flow", "Value") },
            Interactions = new Dictionary<string, InteractionDefinition>
            {
                ["click"] = new("click", "show-panel", new Dictionary<string, string> { ["panel"] = "pipe" })
            },
            ControlVersion = 7,
            Rotation = 12,
            ZIndex = 3,
            IsVisible = false
        };

        var moved = pipe.WithEnd(new PointD(100, 5)).WithStart(new PointD(-5, 10));

        Assert.Equal(new PointD(-5, 10), moved.Start);
        Assert.Equal(new PointD(100, 5), moved.End);
        Assert.Equal(pipe.Bends, moved.Bends);
        Assert.Equal(RectD.FromPoints([new PointD(-5, 10), new PointD(15, 80), new PointD(100, 5)]), moved.Bounds);
        Assert.Equal(pipe.Properties, moved.Properties);
        Assert.Equal(pipe.Bindings, moved.Bindings);
        Assert.Equal(pipe.Interactions, moved.Interactions);
        Assert.Equal(7, moved.ControlVersion);
        Assert.Equal(12, moved.Rotation);
        Assert.Equal(3, moved.ZIndex);
        Assert.False(moved.IsVisible);
    }
}
