using Scada.Controls;
using Scada.Core;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class ControlCatalogTests
{
    [Fact]
    public void CatalogContainsEachApprovedControlFamily()
    {
        var catalog = ControlCatalog.CreateDefault();
        Assert.All(ControlTypeIds.All, id => Assert.NotNull(catalog.Get(id, version: 1)));
    }

    [Fact]
    public void PumpSeparatesCommandAndFeedbackRoles()
    {
        var pump = ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1);
        Assert.Equal(VariableDirection.Command, pump.GetBinding("StartCommand").Direction);
        Assert.Equal(VariableDirection.Feedback, pump.GetBinding("RunFeedback").Direction);
    }

    [Fact]
    public void UnknownControlVersionIsRejectedWithIdentity()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => ControlCatalog.CreateDefault().Get(ControlTypeIds.Vessel, 99));
        Assert.Contains(ControlTypeIds.Vessel, exception.Message, StringComparison.Ordinal);
        Assert.Contains("99", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateRolesAreRejected()
    {
        var boolTypes = new HashSet<VariableDataType> { VariableDataType.Bool };
        Assert.Throws<ArgumentException>(() => new ControlDefinition(
            "test", 1, "test", new(10, 10), new(1, 1), ResizePolicy.Free,
            [],
            [new ControlBindingRole("same", boolTypes, VariableDirection.Feedback, StateSignalRole.None, false),
             new ControlBindingRole("same", boolTypes, VariableDirection.Feedback, StateSignalRole.None, false)],
            []));
    }
}
