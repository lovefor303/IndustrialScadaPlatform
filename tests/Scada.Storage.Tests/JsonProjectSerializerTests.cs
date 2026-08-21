using Scada.Core;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Storage.Tests;

public sealed class JsonProjectSerializerTests
{
    private readonly JsonProjectSerializer _serializer = new();

    [Fact]
    public void SerializeIsDeterministicForTheSameProject()
    {
        var project = CreateSampleProject();

        var first = _serializer.Serialize(project);
        var second = _serializer.Serialize(project);

        Assert.Equal(first, second);
    }

    [Fact]
    public void SerializeAndDeserializeRoundTripsPolymorphicSceneObjects()
    {
        var project = CreateSampleProject();

        var restored = _serializer.Deserialize(_serializer.Serialize(project));
        var objects = restored.Screens.Single().Objects;
        var pipe = Assert.IsType<PipeObject>(objects.Single(sceneObject => sceneObject.Id == project.Screens[0].Objects[1].Id));

        Assert.Equal(project.ProjectId, restored.ProjectId);
        Assert.Equal(project.Name, restored.Name);
        Assert.Equal(project.Variables, restored.Variables);
        Assert.Equal(project.Screens.Single().Name, restored.Screens.Single().Name);
        Assert.Equal(project.Screens.Single().Objects.Count, objects.Count);
        Assert.Equal(new PointD(10, 20), pipe.Start);
        Assert.Equal(new PointD(100, 20), pipe.End);
        Assert.Equal("Main title", Assert.IsType<TextObject>(objects[2]).Text);
        var restoredControl = Assert.IsType<ControlObject>(objects[0]);
        var originalControl = Assert.IsType<ControlObject>(project.Screens.Single().Objects[0]);
        Assert.Equal(originalControl.Dynamics.Keys, restoredControl.Dynamics.Keys);
        var originalDynamic = originalControl.Dynamics["Visibility"];
        var restoredDynamic = restoredControl.Dynamics["Visibility"];
        Assert.Equal(originalDynamic.TargetProperty, restoredDynamic.TargetProperty);
        Assert.Equal(originalDynamic.VariableKey, restoredDynamic.VariableKey);
        Assert.Equal(originalDynamic.ExpectedDataType, restoredDynamic.ExpectedDataType);
        Assert.Equal(originalDynamic.ExpectedDirection, restoredDynamic.ExpectedDirection);
        Assert.Equal(originalDynamic.Condition, restoredDynamic.Condition);
        Assert.Equal(originalDynamic.Mapping, restoredDynamic.Mapping);
        Assert.Equal(originalControl.Interactions.Keys, restoredControl.Interactions.Keys);
        var originalInteraction = originalControl.Interactions["pointer.left.press"];
        var restoredInteraction = restoredControl.Interactions["pointer.left.press"];
        Assert.Equal(originalInteraction.EventName, restoredInteraction.EventName);
        Assert.Equal(originalInteraction.ActionName, restoredInteraction.ActionName);
        Assert.Equal(originalInteraction.Parameters, restoredInteraction.Parameters);
    }

    [Fact]
    public void DeserializeRejectsFutureSchemaVersions()
    {
        const string future = "{\"schemaVersion\":99}";

        Assert.Throws<UnsupportedSchemaVersionException>(() => _serializer.Deserialize(future));
    }

    [Fact]
    public void DeserializeMigratesVersionZeroWithoutInventingBindings()
    {
        var projectId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var versionZero = $$"""
        {
          "schemaVersion": 0,
          "projectId": "{{projectId}}",
          "name": "Legacy Draft",
          "createdAt": "2026-08-19T00:00:00+00:00",
          "updatedAt": "2026-08-19T00:00:00+00:00"
        }
        """;

        var restored = _serializer.Deserialize(versionZero);

        Assert.Equal(ProjectFormat.CurrentVersion, restored.SchemaVersion);
        Assert.Equal(ProjectStatus.Draft, restored.Status);
        Assert.Empty(restored.Screens);
        Assert.Empty(restored.Variables);
    }

    [Fact]
    public void ValidateReturnsErrorsForInvalidJson()
    {
        var errors = _serializer.Validate("{\"schemaVersion\":1,\"name\":\"\"}");

        Assert.NotEmpty(errors);
    }

    private static ProjectDocument CreateSampleProject()
    {
        var control = ControlObject.Create("Valve", new RectD(20, 30, 40, 40)) with
        {
            ZIndex = 2,
            Properties = new Dictionary<string, string> { ["label"] = "XV101" },
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["running"] = new("Valve.RunningFeedback", "fill")
            },
            Dynamics = new Dictionary<string, DynamicDefinition>
            {
                ["Visibility"] = new(
                    "Visibility",
                    "Valve.RunningFeedback",
                    VariableDataType.Bool,
                    VariableDirection.Feedback,
                    condition: "== true")
            },
            Interactions = new Dictionary<string, InteractionDefinition>
            {
                ["pointer.left.press"] = new("pointer.left.press", "command.toggle-bool")
            }
        };
        var pipe = PipeObject.Create(new PointD(10, 20), new PointD(100, 20));
        var text = TextObject.Create("Main title", new RectD(0, 0, 200, 30));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { control, pipe, text });
        var variable = VariableDefinition.Bool("Valve.RunningFeedback", VariableDirection.Feedback);
        return ProjectDocument.Create("Demo", new[] { variable }, new[] { screen });
    }
}
