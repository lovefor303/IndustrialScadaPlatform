using System.Xml.Linq;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Svg;
using Xunit;

namespace Scada.Controls.Svg.Tests;

public sealed class SvgRenderingTests
{
    [Fact]
    public void SvgContainsStableIdentityPartIdsStateQualityAndViewBox()
    {
        var plan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));

        var svg = new SvgControlRenderer().Render(plan);
        var root = XDocument.Parse(svg).Root!;

        Assert.Equal("0 0 120 72", root.Attribute("viewBox")!.Value);
        Assert.Equal(plan.TypeId, root.Attribute("data-control-type")!.Value);
        Assert.Equal("1", root.Attribute("data-control-version")!.Value);
        Assert.Equal("active", root.Attribute("data-state")!.Value);
        Assert.Equal("good", root.Attribute("data-quality")!.Value);
        Assert.Single(root.Descendants(), element =>
            (string?)element.Attribute("data-part") == "pump.casing");
        Assert.Single(root.Descendants(), element =>
            (string?)element.Attribute("data-part") == "pump.motor");
    }

    [Fact]
    public void SvgEscapesLabelsAndDoesNotEmitScriptsOrExternalAssets()
    {
        var context = ControlRenderContext.ForState(ControlState.Active) with
        {
            TextValues = new Dictionary<string, string>
            {
                ["Label"] = "<unsafe>&\" label",
                ["ActionKind"] = "write-command",
                ["ActionTarget"] = "Pump.StartCommand"
            }
        };

        var svg = new SvgControlRenderer().Render(
            PipeInstrumentOperatorGeometry.BuildCommandButton(context));

        Assert.Contains("&lt;unsafe&gt;&amp;\" label", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"http", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xlink:href", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SvgAnimatesOnlyNamedActivePartsAndHonorsReducedMotion()
    {
        var active = new SvgControlRenderer().Render(
            PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active)));
        var reduced = new SvgControlRenderer().Render(
            PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active) with { ReducedMotion = true }));

        Assert.Contains("data-animation=\"pump.rotate\"", active, StringComparison.Ordinal);
        Assert.Contains("data-part=\"pump.impeller\"", active, StringComparison.Ordinal);
        Assert.DoesNotContain("data-animation=\"pump.rotate\"", reduced, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", active, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlPreviewIsDeterministicAndEscapesDocumentMetadata()
    {
        var html = new SvgDocumentWriter().CreateHtml("工业 <预览>",
        [
            new SvgPreviewDocument("pump-active.svg", "泵 <运行>"),
            new SvgPreviewDocument("valve-fault.svg", "阀门故障")
        ]);

        Assert.Contains("工业 &lt;预览&gt;", html, StringComparison.Ordinal);
        Assert.Contains("pump-active.svg", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf("pump-active.svg", StringComparison.Ordinal) < html.IndexOf("valve-fault.svg", StringComparison.Ordinal));
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://example.test/pump.svg")]
    [InlineData("../pump.svg")]
    [InlineData("nested/pump.svg")]
    public void HtmlPreviewRejectsNonLocalSvgFileNames(string fileName)
    {
        var documents = new[] { new SvgPreviewDocument(fileName, "泵") };

        var exception = Assert.Throws<ArgumentException>(() =>
            new SvgDocumentWriter().CreateHtml("预览", documents));

        Assert.Equal("Preview document file names must be local .svg file names without path separators. (Parameter 'documents')", exception.Message);
    }
}
