using Scada.Controls;
using Scada.Core;
using Scada.Runtime;
using Scada.Scene;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeSceneProjectorTests
{
    private static readonly DateTimeOffset SampleTime = new(2026, 8, 23, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProjectionRendersControlsTextAndIndependentPipe()
    {
        var pipe = PipeObject.Create(
            new PointD(80, 50),
            new PointD(320, 50),
            new[] { new PointD(160, 50), new PointD(160, 120) });
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(20, 20, 120, 72)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["RunFeedback"] = new("Pump.Run", "state"),
                ["StopFeedback"] = new("Pump.Stop", "state"),
                ["FaultFeedback"] = new("Pump.Fault", "state"),
                ["StartCommand"] = new("Pump.Start", "command")
            }
        };
        var unsupported = ControlObject.Create("control.unknown", new RectD(400, 20, 50, 50));
        var text = TextObject.Create("配液罐", new RectD(10, 150, 100, 30));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe, pump, unsupported, text });
        var project = PublishedProject(screen);
        var variables = new SimulatedVariableSource(project.Variables);
        variables.Set("Pump.Run", true, VariableQuality.Good, SampleTime);
        variables.Set("Pump.Stop", false, VariableQuality.Good, SampleTime);
        variables.Set("Pump.Fault", false, VariableQuality.Good, SampleTime);
        variables.Set("Pump.Start", false, VariableQuality.Good, SampleTime);

        var runtime = new RuntimeSceneProjector().Project(project, variables, "Main", "desktop", SampleTime);

        Assert.Equal(4, runtime.Objects.Count);
        var renderedPump = Assert.Single(runtime.Objects, item => item.Id == pump.Id);
        Assert.Contains("data-control-type=\"equipment.pump.centrifugal\"", renderedPump.Svg);
        Assert.Contains("data-state=\"active\"", renderedPump.Svg);
        Assert.Contains("data-quality=\"good\"", renderedPump.Svg);
        Assert.Equal(ControlState.Active, renderedPump.State);
        Assert.True(renderedPump.ReadOnly);

        var renderedPipe = Assert.Single(runtime.Objects, item => item.Id == pipe.Id);
        Assert.Equal(pipe.Start, renderedPipe.PipeStart);
        Assert.Equal(pipe.End, renderedPipe.PipeEnd);
        Assert.Equal(pipe.Bends, renderedPipe.PipeBends);
        Assert.Contains("M 80 50 L 160 50 L 160 120 L 320 50", renderedPipe.Svg);

        var placeholder = Assert.Single(runtime.Objects, item => item.Id == unsupported.Id);
        Assert.Contains(placeholder.Diagnostics, diagnostic => diagnostic.Code == RuntimeDiagnostics.ControlUnsupported);
        Assert.Contains(runtime.Diagnostics, diagnostic => diagnostic.Code == RuntimeDiagnostics.ControlUnsupported);
    }

    [Fact]
    public void BadBindingQualityProducesUnknownStateAndMetadata()
    {
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(20, 20, 120, 72)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["RunFeedback"] = new("Pump.Run", "state"),
                ["StopFeedback"] = new("Pump.Stop", "state"),
                ["FaultFeedback"] = new("Pump.Fault", "state")
            }
        };
        var project = PublishedProject(ScreenDocument.Create("Main", new[] { pump }));
        var runtime = new RuntimeSceneProjector().Project(
            project,
            new SimulatedVariableSource(project.Variables),
            "Main",
            "desktop",
            SampleTime);

        var rendered = Assert.Single(runtime.Objects);
        Assert.Equal(ControlState.Unknown, rendered.State);
        Assert.Equal(VariableQuality.Bad, rendered.Quality);
        Assert.Contains("data-quality=\"bad\"", rendered.Svg);
    }

    [Fact]
    public void LayoutProfilesPreserveModelCoordinates()
    {
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(120, 90, 120, 72));
        var project = PublishedProject(ScreenDocument.Create("Main", new[] { pump }));
        var variables = new SimulatedVariableSource(project.Variables);
        var projector = new RuntimeSceneProjector();

        var desktop = projector.Project(project, variables, "Main", "desktop", SampleTime);
        var tablet = projector.Project(project, variables, "Main", "tablet", SampleTime);
        var phone = projector.Project(project, variables, "Main", "phone", SampleTime);

        Assert.Equal("desktop", desktop.LayoutProfile);
        Assert.Equal("tablet", tablet.LayoutProfile);
        Assert.Equal("phone", phone.LayoutProfile);
        Assert.Equal(desktop.Objects.Select(item => item.Bounds), tablet.Objects.Select(item => item.Bounds));
        Assert.Equal(desktop.Objects.Select(item => item.Bounds), phone.Objects.Select(item => item.Bounds));
    }

    private static ProjectDocument PublishedProject(ScreenDocument screen) =>
        ProjectDocument.FromStorage(
            Guid.NewGuid(),
            ProjectFormat.CurrentVersion,
            "Runtime Project",
            SampleTime,
            SampleTime,
            ProjectStatus.Published,
            new[]
            {
                VariableDefinition.Bool("Pump.Run", VariableDirection.Feedback),
                VariableDefinition.Bool("Pump.Stop", VariableDirection.Feedback),
                VariableDefinition.Bool("Pump.Fault", VariableDirection.Feedback),
                VariableDefinition.Bool("Pump.Start", VariableDirection.Command)
            },
            new[] { screen });
}
