using Scada.Runtime;

namespace Scada.Gateway;

internal sealed class RuntimeCoordinatorVariableSource(RuntimeDataCoordinator coordinator) : IRuntimeVariableSource
{
    public RuntimeVariableValue Read(string key, DateTimeOffset now)
    {
        var value = coordinator.GetFullSnapshot([key], now).Values.Single();
        return new RuntimeVariableValue(key, value.Value, value.DataType, value.Quality, value.SourceTimestamp);
    }
}
