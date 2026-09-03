using Scada.Controls;
using Scada.Controls.SampleProject;
using Scada.Scene;
using Xunit;

namespace Scada.Phase2.Acceptance.Tests;

public sealed class SampleProjectTests
{
    [Fact]
    public void GenericSampleContainsEveryInitialFamilyWithoutPlcAddresses()
    {
        var project = SampleProjectFactory.Create();

        Assert.All(
            ControlTypeIds.All,
            typeId => Assert.Contains(project.Screens.SelectMany(screen => screen.Objects), sceneObject => sceneObject.Type == typeId));
        Assert.DoesNotContain(
            project.Screens.SelectMany(screen => screen.Objects).SelectMany(sceneObject => sceneObject.Properties.Values),
            value => value.Contains("DB", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenericProcessScreenIsAFixed1920By1080ReviewCanvas()
    {
        var screen = Assert.Single(SampleProjectFactory.Create().Screens, item => item.Name == SampleProjectFactory.GenericProcessScreenName);
        var canvas = Assert.Single(screen.Objects.OfType<TextObject>(), item => item.Id == SampleProjectFactory.GenericProcessCanvasId);

        Assert.Equal(new RectD(0, 0, 1920, 1080), canvas.Bounds);
        Assert.Contains(SampleProjectFactory.Create().Screens, item => item.Name == SampleProjectFactory.DenseLayoutScreenName);
    }

    [Fact]
    public void GenericSampleKeepsPipesAsIndependentSceneObjects()
    {
        var screen = Assert.Single(SampleProjectFactory.Create().Screens, item => item.Name == SampleProjectFactory.GenericProcessScreenName);

        Assert.All(
            new[] { ControlTypeIds.StraightPipe, ControlTypeIds.PipeElbow, ControlTypeIds.PipeTee },
            typeId => Assert.Contains(screen.Objects.OfType<PipeObject>(), pipe => pipe.Type == typeId));
    }

    [Theory]
    [InlineData("stopped")]
    [InlineData("active")]
    [InlineData("transition")]
    [InlineData("fault")]
    [InlineData("unknown")]
    public void EveryReviewScenarioIsDeterministic(string scenarioName)
    {
        Assert.Equal(SampleScenarios.Get(scenarioName), SampleScenarios.Get(scenarioName));
    }

    [Fact]
    public void ScenarioFeedbackIsReadOnlyAndPendingCommandIsNotConfirmedFeedback()
    {
        var scenario = SampleScenarios.Get("transition");

        Assert.NotEmpty(scenario.PendingActions);
        Assert.DoesNotContain(scenario.Feedback.Values, value => value.Value is string text && text.Contains("command", StringComparison.OrdinalIgnoreCase));
        Assert.All(scenario.Feedback.Keys, key => Assert.DoesNotContain("Command", key, StringComparison.Ordinal));
    }
}
