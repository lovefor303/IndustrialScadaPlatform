using Json.Schema;
using Scada.Core;
using Scada.Scene;
using System.Text.Json;
using Xunit;

namespace Scada.Core.Tests;

public sealed class ContractTests
{
    private static readonly Lazy<JsonSchema> ProjectSchema = new(LoadProjectSchema);

    [Fact]
    public void NewProjectHasStableIdAndSchemaVersion()
    {
        var project = ProjectDocument.Create("Demo");

        Assert.NotEqual(Guid.Empty, project.ProjectId);
        Assert.Equal(ProjectFormat.CurrentVersion, project.SchemaVersion);
        Assert.Equal("Demo", project.Name);
        Assert.Equal(ProjectStatus.Draft, project.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectNameMustNotBeBlank(string name)
    {
        Assert.Throws<ArgumentException>(() => ProjectDocument.Create(name));
    }

    [Fact]
    public void CommandAndFeedbackVariablesAreDistinct()
    {
        var command = VariableDefinition.Bool("Pump.StartCommand", VariableDirection.Command);
        var feedback = VariableDefinition.Bool("Pump.RunningFeedback", VariableDirection.Feedback);

        Assert.NotEqual(command.Key, feedback.Key);
        Assert.Equal(VariableDirection.Command, command.Direction);
        Assert.Equal(VariableDirection.Feedback, feedback.Direction);
    }

    [Fact]
    public void BadQualityIsExplicitAndNotAConfirmedValue()
    {
        var value = VariableValue.Unknown(VariableDataType.Bool, DateTimeOffset.UnixEpoch);

        Assert.Equal(VariableQuality.Bad, value.Quality);
        Assert.Null(value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Pump Start")]
    [InlineData("Pump/Start")]
    public void VariableKeyMustUseStableSupportedCharacters(string key)
    {
        Assert.Throws<ArgumentException>(() =>
            VariableDefinition.Bool(key, VariableDirection.Feedback));
    }

    [Fact]
    public void VariableMinimumMustNotExceedMaximum()
    {
        Assert.Throws<ArgumentException>(() =>
            VariableDefinition.Number(
                "Tank.Level",
                VariableDataType.Float64,
                VariableDirection.Feedback,
                "percent",
                minimum: 100,
                maximum: 0));
    }

    [Fact]
    public void DuplicateProjectVariableKeysAreRejected()
    {
        var first = VariableDefinition.Bool("Pump.Running", VariableDirection.Feedback);
        var duplicate = VariableDefinition.Bool("Pump.Running", VariableDirection.Command);

        Assert.Throws<ArgumentException>(() =>
            ProjectDocument.Create("Demo", new[] { first, duplicate }));
    }

    [Fact]
    public void PipeIsIndependentFromEquipmentObjects()
    {
        var valve = SceneObject.Create("Valve", new RectD(10, 20, 40, 40));
        var pipe = PipeObject.Create(new PointD(0, 40), new PointD(100, 40));
        var scene = ScreenDocument.Create("Main", new[] { valve, pipe });

        var moved = valve with { Bounds = valve.Bounds with { X = 30 } };
        var updated = scene.ReplaceObject(moved);
        var updatedPipe = Assert.IsType<PipeObject>(updated.Objects.Single(o => o.Id == pipe.Id));

        Assert.Equal(0, updatedPipe.Start.X);
        Assert.Equal(100, updatedPipe.End.X);
    }

    [Fact]
    public void DuplicateSceneObjectIdsAreRejected()
    {
        var id = Guid.NewGuid();
        var first = SceneObject.Create("Valve", new RectD(0, 0, 10, 10), id);
        var second = SceneObject.Create("Pump", new RectD(20, 0, 10, 10), id);

        Assert.Throws<ArgumentException>(() => ScreenDocument.Create("Main", new[] { first, second }));
    }

    [Fact]
    public void ProjectCanContainScreensAndVariables()
    {
        var variable = VariableDefinition.Bool("Tank.Running", VariableDirection.Feedback);
        var control = ControlObject.Create("Tank", new RectD(10, 20, 120, 180));
        var screen = ScreenDocument.Create("Main", new[] { control });
        var project = ProjectDocument.Create("Demo", new[] { variable }, new[] { screen });

        Assert.Single(project.Screens);
        Assert.Equal(control.Id, project.Screens[0].Objects[0].Id);
        Assert.Equal(variable.Key, project.Variables[0].Key);
    }

    [Fact]
    public void ProjectSchemaAcceptsTheValidFixture()
    {
        var result = EvaluateFixture("empty-project.json");

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void ProjectSchemaRejectsMissingVersionAndBlankName()
    {
        var result = EvaluateFixture("invalid-project.json");

        Assert.False(result.IsValid);
        Assert.True(result.Details?.Count >= 2, result.ToString());
    }

    private static EvaluationResults EvaluateFixture(string fixtureName)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var fixtureText = File.ReadAllText(Path.Combine(
            root,
            "docs",
            "superpowers",
            "plans",
            "phase1-test-fixtures",
            fixtureName));
        using var document = JsonDocument.Parse(fixtureText);

        return ProjectSchema.Value.Evaluate(document.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });
    }

    private static JsonSchema LoadProjectSchema()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var schemaText = File.ReadAllText(Path.Combine(root, "schemas", "project-v1.schema.json"));
        return JsonSchema.FromText(schemaText);
    }
}
