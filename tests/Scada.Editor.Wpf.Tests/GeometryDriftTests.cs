using Scada.Controls;
using Scada.Core;
using Scada.Scene;
using Scada.Storage;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class GeometryDriftTests
{
    [Fact]
    public void SceneRoundTripPreservesGeometryAndEditorMetadataByteForByte()
    {
        var control = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(120, 80, 100, 80)) with
        {
            Rotation = 22.5,
            ZIndex = 4,
            IsVisible = false,
            Properties = new Dictionary<string, string> { ["label"] = "XV101" },
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["OpenFeedback"] = new("Valve.OpenFeedback", "state")
            },
            Dynamics = new Dictionary<string, DynamicDefinition>
            {
                ["Opacity"] = new(
                    "Opacity",
                    "Valve.OpenFeedback",
                    VariableDataType.Bool,
                    VariableDirection.Feedback,
                    mapping: new Dictionary<string, string> { ["true"] = "1", ["false"] = "0.4" })
            },
            Interactions = new Dictionary<string, InteractionDefinition>
            {
                ["pointer.left.press"] = new("pointer.left.press", "message.show", new Dictionary<string, string> { ["text"] = "测试" })
            }
        };
        var pipe = PipeObject.Create(
            new PointD(20, 120),
            new PointD(420, 120),
            new[] { new PointD(80, 120), new PointD(80, 200), new PointD(300, 200) });
        var project = ProjectDocument.Create(
            "Drift gate",
            new[] { VariableDefinition.Bool("Valve.OpenFeedback", VariableDirection.Feedback) },
            new[] { ScreenDocument.Create("Main", new SceneObject[] { control, pipe, TextObject.Create("标题", new RectD(10, 10, 100, 30)) }) });
        var serializer = new JsonProjectSerializer();

        var json = serializer.Serialize(project);
        var restored = serializer.Deserialize(json);

        Assert.Equal(json, serializer.Serialize(restored));
    }
}
