using Scada.Core;

namespace Scada.Runtime;

public enum RuntimeDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record RuntimeDiagnostic(
    string Code,
    string Message,
    Guid? ObjectId,
    RuntimeDiagnosticSeverity Severity);

public static class RuntimeDiagnostics
{
    public const string ProjectMissing = "project.missing";
    public const string ProjectInvalid = "project.invalid";
    public const string ProjectNotPublished = "project.not-published";
    public const string ScreenNotFound = "screen.not-found";
    public const string VariableUnknown = "variable.unknown";
    public const string ControlUnsupported = "control.unsupported";
    public const string SourceDisconnected = "source.disconnected";

    public static RuntimeDiagnostic Create(
        string code,
        string message,
        Guid? objectId = null,
        RuntimeDiagnosticSeverity severity = RuntimeDiagnosticSeverity.Error) =>
        new(code, message, objectId, severity);

    public static RuntimeDiagnostic UnknownVariable(string key) =>
        Create(VariableUnknown, $"变量“{key}”不存在或当前没有有效质量。", severity: RuntimeDiagnosticSeverity.Warning);
}
