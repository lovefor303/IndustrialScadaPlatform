using Scada.Core;
using Scada.Simulator;
using Xunit;

namespace Scada.Simulator.Tests;

public sealed class OfflineSimulatorTests
{
    private static readonly DateTimeOffset SampleTime = new(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SameSeedAndTimeProduceRepeatableNumericValue()
    {
        var definition = VariableDefinition.Number(
            "Tank.Level",
            VariableDataType.Float64,
            VariableDirection.Feedback,
            "percent",
            0,
            100);
        var first = new OfflineSimulator(new[] { definition }, seed: 42);
        var second = new OfflineSimulator(new[] { definition }, seed: 42);

        var firstValue = first.Read(definition.Key, SampleTime);
        var secondValue = second.Read(definition.Key, SampleTime);

        Assert.Equal(firstValue, secondValue);
        Assert.InRange(Assert.IsType<double>(firstValue.Value), 0, 100);
        Assert.Equal(VariableQuality.Good, firstValue.Quality);
    }

    [Fact]
    public void BoolFeedbackDefaultsToFalse()
    {
        var definition = VariableDefinition.Bool("Pump.Running", VariableDirection.Feedback);
        var simulator = new OfflineSimulator(new[] { definition }, seed: 1);

        var value = simulator.Read(definition.Key, SampleTime);

        Assert.False(Assert.IsType<bool>(value.Value));
        Assert.Equal(VariableQuality.Good, value.Quality);
    }

    [Fact]
    public void UnknownVariableReturnsBadQualityWithoutAValue()
    {
        var simulator = new OfflineSimulator(Array.Empty<VariableDefinition>(), seed: 1);

        var value = simulator.Read("Missing.Variable", SampleTime);

        Assert.Equal(VariableQuality.Bad, value.Quality);
        Assert.Null(value.Value);
    }

    [Fact]
    public void SetFeedbackOverridesGeneratedValueAndQuality()
    {
        var definition = VariableDefinition.Number(
            "Tank.Level",
            VariableDataType.Float64,
            VariableDirection.Feedback,
            minimum: 0,
            maximum: 100);
        var simulator = new OfflineSimulator(new[] { definition }, seed: 1);

        simulator.SetFeedback(definition.Key, 56.5, VariableQuality.Uncertain);
        var value = simulator.Read(definition.Key, SampleTime);

        Assert.Equal(56.5, value.Value);
        Assert.Equal(VariableQuality.Uncertain, value.Quality);
        Assert.Equal(SampleTime, value.Timestamp);
    }

    [Fact]
    public void SimulatorRejectsWritesToCommandVariables()
    {
        var command = VariableDefinition.Bool("Pump.StartCommand", VariableDirection.Command);
        var simulator = new OfflineSimulator(new[] { command }, seed: 1);

        Assert.Throws<InvalidOperationException>(() => simulator.SetFeedback(command.Key, true));
        Assert.Equal(VariableQuality.Bad, simulator.Read(command.Key, SampleTime).Quality);
    }

    [Fact]
    public void SimulatorRejectsOutOfRangeOrWrongTypeFeedback()
    {
        var definition = VariableDefinition.Number(
            "Tank.Level",
            VariableDataType.Float64,
            VariableDirection.Feedback,
            minimum: 0,
            maximum: 100);
        var simulator = new OfflineSimulator(new[] { definition }, seed: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => simulator.SetFeedback(definition.Key, 101d));
        Assert.Throws<ArgumentException>(() => simulator.SetFeedback(definition.Key, "high"));
    }

    [Fact]
    public void SnapshotContainsEveryDefinitionWithCurrentTimestamp()
    {
        var running = VariableDefinition.Bool("Pump.Running", VariableDirection.Feedback);
        var level = VariableDefinition.Number(
            "Tank.Level",
            VariableDataType.Float64,
            VariableDirection.Feedback,
            minimum: 0,
            maximum: 100);
        var simulator = new OfflineSimulator(new[] { running, level }, seed: 8);

        var snapshot = simulator.Snapshot(SampleTime);

        Assert.Equal(2, snapshot.Count);
        Assert.All(snapshot.Values, value => Assert.Equal(SampleTime, value.Timestamp));
    }
}
