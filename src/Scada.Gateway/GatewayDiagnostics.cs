using Scada.Runtime;

namespace Scada.Gateway;

public static class GatewayDiagnostics
{
    public const string AuthRequired = "AUTH_REQUIRED";
    public const string AccessDenied = "ACCESS_DENIED";

    public static RuntimeDiagnostic AuthenticationFailed(string? userName = null, string? password = null) =>
        RuntimeDiagnostics.Create(AuthRequired, "需要登录后才能访问运行时。", severity: RuntimeDiagnosticSeverity.Warning);

    public static RuntimeDiagnostic AccessDeniedDiagnostic() =>
        RuntimeDiagnostics.Create(AccessDenied, "当前账户没有执行此操作的权限。", severity: RuntimeDiagnosticSeverity.Warning);
}
