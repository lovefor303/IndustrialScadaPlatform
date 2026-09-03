using Scada.Controls;
using Scada.Core;
using Scada.Runtime;
using Scada.Scene;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeContractTests
{
    [Fact]
    public void RuntimeVariableValuePreservesUnknownQualityWithoutFabricatingValue()
    {
        var timestamp = new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);

        var value = new RuntimeVariableValue(
            "Tank.Level",
            null,
            VariableDataType.Float64,
            VariableQuality.Bad,
            timestamp);

        Assert.Equal("Tank.Level", value.Key);
        Assert.Null(value.Value);
        Assert.Equal(VariableQuality.Bad, value.Quality);
        Assert.Equal(timestamp, value.Timestamp);
    }

    [Fact]
    public void RuntimeObjectProjectionCarriesGeometryAndReadOnlyPolicy()
    {
        var id = Guid.NewGuid();
        var bounds = new RectD(10, 20, 120, 80);

        var projection = new RuntimeObjectProjection(
            id,
            "pump.centrifugal",
            bounds,
            15,
            7,
            true,
            "<svg data-control-type=\"pump.centrifugal\" />",
            ControlState.Active,
            VariableQuality.Good,
            ReadOnly: true,
            Diagnostics: []);

        Assert.Equal(id, projection.Id);
        Assert.Equal(bounds, projection.Bounds);
        Assert.Equal(15, projection.Rotation);
        Assert.Equal(7, projection.ZIndex);
        Assert.True(projection.ReadOnly);
        Assert.Empty(projection.Diagnostics);
    }

    [Fact]
    public void RuntimeScreenDocumentRetainsScreenNameAndProfile()
    {
        var document = new RuntimeScreenDocument(
            "Main",
            new RectD(0, 0, 1920, 1080),
            "desktop",
            [],
            []);

        Assert.Equal("Main", document.Name);
        Assert.Equal("desktop", document.LayoutProfile);
        Assert.Empty(document.Objects);
        Assert.Empty(document.Diagnostics);
    }

    [Fact]
    public void RuntimeDiagnosticUsesStableCodeAndChineseMessage()
    {
        var diagnostic = new RuntimeDiagnostic(
            "project.not-published",
            "项目尚未发布，不能进入运行时。",
            null,
            RuntimeDiagnosticSeverity.Error);

        Assert.Equal("project.not-published", diagnostic.Code);
        Assert.Contains("尚未发布", diagnostic.Message);
        Assert.Equal(RuntimeDiagnosticSeverity.Error, diagnostic.Severity);
    }
}
