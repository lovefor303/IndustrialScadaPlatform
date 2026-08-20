using Scada.Controls;
using Scada.Core;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class ControlStateResolverTests
{
    private static readonly DateTimeOffset SampleTime = new(2026, 8, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly ControlDefinition PumpDefinition = ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1);

    [Theory]
    [InlineData(VariableQuality.Bad, false, false, false, false, ControlState.Unknown)]
    [InlineData(VariableQuality.Good, true, true, false, false, ControlState.Fault)]
    [InlineData(VariableQuality.Good, false, true, true, false, ControlState.Fault)]
    [InlineData(VariableQuality.Good, false, false, false, true, ControlState.Transition)]
    [InlineData(VariableQuality.Good, false, true, false, false, ControlState.Active)]
    [InlineData(VariableQuality.Good, false, false, true, false, ControlState.Stopped)]
    public void ResolverAppliesRequiredPrecedence(
        VariableQuality quality,
        bool fault,
        bool active,
        bool inactive,
        bool commandPending,
        ControlState expected)
    {
        var snapshot = CreatePumpSnapshot(quality, fault, active, inactive, commandPending);
        var result = ControlStateResolver.Resolve(PumpDefinition, snapshot, SampleTime);
        Assert.Equal(expected, result.State);
    }

    [Fact]
    public void StartCommandWithoutRunFeedbackIsNotActive()
    {
        var result = ControlStateResolver.Resolve(
            PumpDefinition,
            CreatePumpSnapshot(VariableQuality.Good, fault: false, active: false, inactive: false, commandPending: true),
            SampleTime);
        Assert.Equal(ControlState.Transition, result.State);
        Assert.DoesNotContain("pump.rotate", result.ActiveAnimations);
    }

    [Fact]
    public void StaleRequiredFeedbackBecomesUnknown()
    {
        var snapshot = CreatePumpSnapshot(VariableQuality.Good, fault: false, active: true, inactive: false, commandPending: false)
            .ToDictionary(pair => pair.Key, pair => pair.Value with { Timestamp = SampleTime.AddSeconds(-6) });
        var result = ControlStateResolver.Resolve(PumpDefinition, snapshot, SampleTime);
        Assert.Equal(ControlState.Unknown, result.State);
    }

    [Fact]
    public void ActiveFeedbackEnablesOnlyNamedAnimation()
    {
        var result = ControlStateResolver.Resolve(
            PumpDefinition,
            CreatePumpSnapshot(VariableQuality.Good, fault: false, active: true, inactive: false, commandPending: false),
            SampleTime);
        Assert.Equal(ControlState.Active, result.State);
        Assert.Contains("pump.rotate", result.ActiveAnimations);
    }

    private static Dictionary<string, VariableValue> CreatePumpSnapshot(
        VariableQuality quality,
        bool fault,
        bool active,
        bool inactive,
        bool commandPending) =>
        new Dictionary<string, VariableValue>
        {
            ["FaultFeedback"] = BoolValue(fault, quality),
            ["RunFeedback"] = BoolValue(active, quality),
            ["StopFeedback"] = BoolValue(inactive, quality),
            ["StartCommand"] = BoolValue(commandPending, VariableQuality.Good)
        };

    private static VariableValue BoolValue(bool value, VariableQuality quality) =>
        new(VariableDataType.Bool, value, quality, SampleTime);
}
