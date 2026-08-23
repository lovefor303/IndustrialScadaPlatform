using Scada.Controls;
using Scada.Core;
using Scada.Scene;

namespace Scada.Runtime;

public interface IRuntimeProjectSource
{
    Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default);
}

public interface IRuntimeVariableSource
{
    RuntimeVariableValue Read(string key, DateTimeOffset now);
}

public sealed record RuntimeVariableValue(
    string Key,
    object? Value,
    VariableDataType DataType,
    VariableQuality Quality,
    DateTimeOffset Timestamp);

public sealed record RuntimeProjectMetadata(
    Guid ProjectId,
    string Name,
    ProjectStatus Status,
    IReadOnlyList<string> Screens,
    DateTimeOffset UpdatedAt);

public sealed record RuntimeObjectProjection(
    Guid Id,
    string Type,
    RectD Bounds,
    double Rotation,
    int ZIndex,
    bool IsVisible,
    string? Svg,
    ControlState State,
    VariableQuality Quality,
    bool ReadOnly,
    IReadOnlyList<RuntimeDiagnostic> Diagnostics);

public sealed record RuntimeScreenDocument(
    string Name,
    RectD DesignBounds,
    string LayoutProfile,
    IReadOnlyList<RuntimeObjectProjection> Objects,
    IReadOnlyList<RuntimeDiagnostic> Diagnostics);
