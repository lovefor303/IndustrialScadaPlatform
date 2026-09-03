using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Svg;
using Scada.Controls.Wpf;
using Xunit;

namespace Scada.Phase2.Acceptance.Tests;

public sealed class CrossRendererAcceptanceTests
{
    [Theory]
    [InlineData(ControlState.Stopped)]
    [InlineData(ControlState.Active)]
    [InlineData(ControlState.Transition)]
    [InlineData(ControlState.Fault)]
    [InlineData(ControlState.Unknown)]
    public async Task EveryControlHasEquivalentWpfAndSvgSemantics(ControlState state)
    {
        foreach (var typeId in ControlTypeIds.All)
        {
            var context = ControlRenderContext.ForState(state) with
            {
                Quality = state == ControlState.Unknown ? Scada.Core.VariableQuality.Bad : Scada.Core.VariableQuality.Good
            };
            var plan = ControlGeometryFactory.Build(typeId, context);

            var wpf = await StaThread.RunAsync(() => CaptureWpf(WpfControlRenderer.Render(plan)));
            var svg = CaptureSvg(new SvgControlRenderer().Render(plan));

            AssertSemanticsEqual(wpf, svg);
            Assert.NotEmpty(wpf.PartIds);
            Assert.DoesNotContain(wpf.PartIds, partId => partId.Contains("placeholder", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static RendererSemantics CaptureWpf(DependencyObject root)
    {
        var parts = FindVisuals(root)
            .Select(element => WpfControlRenderer.GetPartId(element))
            .Where(partId => !string.IsNullOrWhiteSpace(partId) && partId != "control.root")
            .ToHashSet(StringComparer.Ordinal);
        var visibleParts = FindVisuals(root)
            .Where(element => element is UIElement { Visibility: Visibility.Visible })
            .Select(element => WpfControlRenderer.GetPartId(element))
            .Where(partId => !string.IsNullOrWhiteSpace(partId) && partId != "control.root")
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(visibleParts);

        var activeAnimationParts = FindVisuals(root)
            .Where(element => element is UIElement { RenderTransform: not null } uiElement
                && !ReferenceEquals(uiElement.RenderTransform, Transform.Identity))
            .Select(element => WpfControlRenderer.GetPartId(element))
            .Where(partId => !string.IsNullOrWhiteSpace(partId))
            .ToHashSet(StringComparer.Ordinal);

        return new RendererSemantics(
            WpfControlRenderer.GetControlTypeId(root),
            WpfControlRenderer.GetControlVersion(root),
            WpfControlRenderer.GetControlState(root),
            WpfControlRenderer.GetQuality(root),
            parts,
            activeAnimationParts,
            WpfControlRenderer.GetAnchors(root));
    }

    private static RendererSemantics CaptureSvg(string svg)
    {
        var document = XDocument.Parse(svg);
        var root = document.Root!;
        var parts = root.Descendants()
            .Select(element => (string?)element.Attribute("data-part"))
            .Where(partId => !string.IsNullOrWhiteSpace(partId))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        var activeAnimationParts = root.Descendants()
            .Where(element => element.Attribute("data-animation") is not null)
            .Select(element => (string?)element.Attribute("data-part"))
            .Where(partId => !string.IsNullOrWhiteSpace(partId))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        var anchors = root.Descendants().Where(element => element.Name.LocalName == "anchor")
            .ToDictionary(
                element => element.Attribute("data-anchor")!.Value,
                element => new RenderPoint(
                    double.Parse(element.Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(element.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture)),
                StringComparer.Ordinal);

        return new RendererSemantics(
            root.Attribute("data-control-type")!.Value,
            int.Parse(root.Attribute("data-control-version")!.Value, System.Globalization.CultureInfo.InvariantCulture),
            Enum.Parse<ControlState>(root.Attribute("data-state")!.Value, ignoreCase: true),
            root.Attribute("data-quality")!.Value == "bad" ? Scada.Core.VariableQuality.Bad : Scada.Core.VariableQuality.Good,
            parts,
            activeAnimationParts,
            anchors);
    }

    private static IEnumerable<DependencyObject> FindVisuals(DependencyObject root)
    {
        yield return root;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var child in FindVisuals(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static void AssertSemanticsEqual(RendererSemantics wpf, RendererSemantics svg)
    {
        Assert.Equal(svg.TypeId, wpf.TypeId);
        Assert.Equal(svg.Version, wpf.Version);
        Assert.Equal(svg.State, wpf.State);
        Assert.Equal(svg.Quality, wpf.Quality);
        Assert.Equal(svg.PartIds.OrderBy(value => value, StringComparer.Ordinal), wpf.PartIds.OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(
            svg.ActiveAnimationPartIds.OrderBy(value => value, StringComparer.Ordinal),
            wpf.ActiveAnimationPartIds.OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(
            svg.Anchors.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            wpf.Anchors.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    private sealed record RendererSemantics(
        string TypeId,
        int Version,
        ControlState State,
        Scada.Core.VariableQuality Quality,
        IReadOnlySet<string> PartIds,
        IReadOnlySet<string> ActiveAnimationPartIds,
        IReadOnlyDictionary<string, RenderPoint> Anchors);
}
