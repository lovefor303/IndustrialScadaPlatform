using Scada.Controls;
using Scada.Controls.Rendering;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class RenderContractTests
{
    [Fact]
    public void AnchorOutsideDesignBoundsIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRenderPlan(
            "test.control", 1, new RenderSize(100, 60), Array.Empty<RenderPrimitive>(),
            new Dictionary<string, RenderPoint> { ["outside"] = new(101, 30) },
            ControlState.Neutral, new HashSet<string>(), new Dictionary<string, string>()));
    }

    [Fact]
    public void DuplicatePrimitivePartsAreRejected()
    {
        var primitives = new RenderPrimitive[]
        {
            new RenderLine("pipe", new(0, 0), new(10, 0), VisualTokens.Outline),
            new RenderLine("pipe", new(0, 1), new(10, 1), VisualTokens.Outline)
        };

        Assert.Throws<ArgumentException>(() => new ControlRenderPlan(
            "test.control", 1, new RenderSize(100, 60), primitives,
            new Dictionary<string, RenderPoint>(), ControlState.Neutral,
            new HashSet<string>(), new Dictionary<string, string>()));
    }

    [Fact]
    public void EquipmentPreservesAspectRatioButStraightPipeStretchesHorizontally()
    {
        var catalog = ControlCatalog.CreateDefault();
        Assert.Equal(ResizePolicy.PreserveAspectRatio, catalog.Get(ControlTypeIds.CentrifugalPump, 1).ResizePolicy);
        Assert.Equal(ResizePolicy.StretchHorizontal, catalog.Get(ControlTypeIds.StraightPipe, 1).ResizePolicy);
    }

    [Fact]
    public void NormalTokensAreQuieterThanFaultTokens()
    {
        Assert.NotEqual(VisualTokens.EquipmentBody, VisualTokens.Fault);
        Assert.Equal("#D64045", VisualTokens.Fault);
        Assert.Equal(1.5, VisualTokens.EquipmentStroke);
        Assert.Equal(4.0, VisualTokens.PipeStroke);
    }

    [Fact]
    public void RenderContextClampsNumericValuesAndPreservesQuality()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with
        {
            NumericValues = new Dictionary<string, double> { ["Fill"] = 125 },
            Quality = Scada.Core.VariableQuality.Bad,
            ReducedMotion = true
        };

        Assert.Equal(100, context.GetClampedNumeric("Fill", 0, 100));
        Assert.Equal(Scada.Core.VariableQuality.Bad, context.Quality);
        Assert.True(context.ReducedMotion);
    }
}
