using System.Xml.Linq;
using System.Globalization;
using Scada.Controls;
using Scada.Controls.SampleProject;
using Scada.Controls.Svg;
using Scada.Controls.Wpf;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Phase2.Acceptance.Tests;

public sealed class Phase2AcceptanceTests
{
    [Fact]
    public async Task GenericSampleRoundTripsAndKeepsEveryScenarioRendererSemanticallyEquivalent()
    {
        var project = SampleProjectFactory.Create();
        var serializer = new JsonProjectSerializer();
        var restored = serializer.Deserialize(serializer.Serialize(project));
        var catalog = ControlCatalog.CreateDefault();
        var svgRenderer = new SvgControlRenderer();

        Assert.Equal(2, restored.SchemaVersion);
        Assert.Equal(project.ProjectId, restored.ProjectId);

        foreach (var control in restored.Screens.SelectMany(screen => screen.Objects).OfType<ControlObject>())
        {
            var definition = catalog.Get(control.Type, control.ControlVersion);
            Assert.Empty(ControlValidator.Validate(control, definition, restored.Variables, new HashSet<string>()));
        }

        foreach (var scenarioName in new[] { "stopped", "active", "transition", "fault", "unknown" })
        {
            foreach (var renderable in SampleProjectFactory.BuildRenderables(scenarioName))
            {
                var wpf = await StaThread.RunAsync(() =>
                {
                    var root = WpfControlRenderer.Render(renderable.Plan);
                    return (WpfControlRenderer.GetControlTypeId(root), WpfControlRenderer.GetControlVersion(root));
                });
                var svg = XDocument.Parse(svgRenderer.Render(renderable.Plan)).Root!;

                Assert.Equal(renderable.Plan.TypeId, wpf.Item1);
                Assert.Equal(renderable.Plan.Version, wpf.Item2);
                Assert.Equal(renderable.Plan.State.ToString().ToLowerInvariant(), svg.Attribute("data-state")!.Value);
                Assert.Equal(renderable.Plan.TypeId, svg.Attribute("data-control-type")!.Value);
                Assert.Equal(renderable.Plan.Version.ToString(CultureInfo.InvariantCulture), svg.Attribute("data-control-version")!.Value);
            }
        }
    }

    [Fact]
    public void InvalidBindingHasStableDiagnosticAndDoesNotPreventOtherControlValidation()
    {
        var project = SampleProjectFactory.Create();
        var catalog = ControlCatalog.CreateDefault();
        var controls = project.Screens.SelectMany(screen => screen.Objects).OfType<ControlObject>().ToArray();
        var invalidPump = controls.Single(control => control.Type == ControlTypeIds.CentrifugalPump) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["RunFeedback"] = new("missing.variable", "RunFeedback")
            }
        };

        var errors = ControlValidator.Validate(invalidPump, catalog.Get(invalidPump.Type, invalidPump.ControlVersion), project.Variables, new HashSet<string>());
        var validValve = controls.Single(control => control.Type == ControlTypeIds.AutomatedValve);
        var nextErrors = ControlValidator.Validate(validValve, catalog.Get(validValve.Type, validValve.ControlVersion), project.Variables, new HashSet<string>());

        Assert.Contains(errors, error => error.Code == "control.binding.missing-variable" && error.Path == "bindings.RunFeedback");
        Assert.Empty(nextErrors);
    }

    [Fact]
    public void ReusableDefinitionsContainNoSiemensDbAddressStrings()
    {
        var catalog = ControlCatalog.CreateDefault();
        var text = string.Join(
            "\n",
            ControlTypeIds.All.Select(typeId => catalog.Get(typeId, 1)).Select(definition => string.Join(
                "|",
                definition.TypeId,
                definition.DisplayNameKey,
                string.Join("|", definition.Properties.Select(property => property.DefaultValue)),
                string.Join("|", definition.Bindings.Select(binding => binding.Name)))));

        Assert.DoesNotMatch(@"(?i)\bDB\s*\d+", text);
        Assert.DoesNotContain("%I", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%Q", text, StringComparison.OrdinalIgnoreCase);
    }
}
