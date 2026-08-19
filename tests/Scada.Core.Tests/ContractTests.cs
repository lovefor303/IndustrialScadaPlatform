using Scada.Core;
using Scada.Scene;
using Xunit;

namespace Scada.Core.Tests;

public sealed class ContractTests
{
    [Fact]
    public void NewProjectHasStableIdAndSchemaVersion()
    {
        var project = ProjectDocument.Create("Demo");

        Assert.NotEqual(Guid.Empty, project.ProjectId);
        Assert.Equal(1, project.SchemaVersion);
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
}
