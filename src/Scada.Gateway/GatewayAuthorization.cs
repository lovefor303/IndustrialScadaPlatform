namespace Scada.Gateway;

public enum GatewayCapability
{
    RuntimeView,
    GatewayManage
}

public static class GatewayAuthorization
{
    public static bool HasPermission(AuthRole role, GatewayCapability permission) =>
        permission switch
        {
            GatewayCapability.RuntimeView => true,
            GatewayCapability.GatewayManage => role == AuthRole.Admin,
            _ => false
        };
}
