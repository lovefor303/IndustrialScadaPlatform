using Scada.Core;
using Scada.Runtime;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class SimulatedVariableSourceTests
{
    private static readonly DateTimeOffset SampleTime = new(2026, 8, 23, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    public void ExplicitValueRetainsDeclaredTypeAndQuality()
    {
        var source = new SimulatedVariableSource(new[]
        {
            VariableDefinition.Number("Tank.Level", VariableDataType.Float64, VariableDirection.Feedback),
            VariableDefinition.Bool("Pump.Run", VariableDirection.Feedback),
            new VariableDefinition("Batch.Name", VariableDataType.String, VariableDirection.Parameter)
        });

        source.Set("Tank.Level", 42.5, VariableQuality.Good, SampleTime);
        source.Set("Pump.Run", true, VariableQuality.Uncertain, SampleTime);
        source.Set("Batch.Name", "Demo", VariableQuality.Good, SampleTime);

        Assert.Equal((42.5, VariableDataType.Float64, VariableQuality.Good),
            (source.Read("Tank.Level", SampleTime).Value, source.Read("Tank.Level", SampleTime).DataType, source.Read("Tank.Level", SampleTime).Quality));
        Assert.Equal((true, VariableDataType.Bool, VariableQuality.Uncertain),
            (source.Read("Pump.Run", SampleTime).Value, source.Read("Pump.Run", SampleTime).DataType, source.Read("Pump.Run", SampleTime).Quality));
        Assert.Equal(("Demo", VariableDataType.String, VariableQuality.Good),
            (source.Read("Batch.Name", SampleTime).Value, source.Read("Batch.Name", SampleTime).DataType, source.Read("Batch.Name", SampleTime).Quality));
    }

    [Fact]
    public void UnknownKeyReturnsBadQualityAndNullValue()
    {
        var source = new SimulatedVariableSource(Array.Empty<VariableDefinition>());

        var value = source.Read("Missing", SampleTime);

        Assert.Equal("Missing", value.Key);
        Assert.Null(value.Value);
        Assert.Equal(VariableQuality.Bad, value.Quality);
        Assert.Equal(SampleTime, value.Timestamp);
    }

    [Fact]
    public void SetRejectsValueWithWrongDeclaredType()
    {
        var source = new SimulatedVariableSource(new[]
        {
            VariableDefinition.Bool("Pump.Run", VariableDirection.Feedback)
        });

        Assert.Throws<ArgumentException>(() =>
        {
            source.Set("Pump.Run", 1, VariableQuality.Good, SampleTime);
        });
    }

    [Fact]
    public void ReadsAreDeterministicAndDoNotMutateDefinitions()
    {
        var definitions = new[]
        {
            VariableDefinition.Number("Tank.Level", VariableDataType.Float64, VariableDirection.Feedback)
        };
        var source = new SimulatedVariableSource(definitions);
        source.Set("Tank.Level", 12.25, VariableQuality.Good, SampleTime);

        var first = source.Read("Tank.Level", SampleTime.AddMinutes(1));
        var second = source.Read("Tank.Level", SampleTime.AddMinutes(1));

        Assert.Equal(first, second);
        Assert.Single(definitions);
        Assert.Equal("Tank.Level", definitions[0].Key);
    }
}
