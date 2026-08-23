using Scada.Gateway;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class AuthorizationTests
{
    [Theory]
    [InlineData(AuthRole.Viewer, true)]
    [InlineData(AuthRole.Operator, true)]
    [InlineData(AuthRole.Engineer, true)]
    [InlineData(AuthRole.Admin, true)]
    public void RuntimeViewIsGrantedToAllRuntimeRoles(AuthRole role, bool expected)
    {
        Assert.Equal(expected, GatewayAuthorization.HasPermission(role, GatewayCapability.RuntimeView));
    }

    [Theory]
    [InlineData(AuthRole.Viewer, false)]
    [InlineData(AuthRole.Operator, false)]
    [InlineData(AuthRole.Engineer, false)]
    [InlineData(AuthRole.Admin, true)]
    public void GatewayManagementIsAdminOnly(AuthRole role, bool expected)
    {
        Assert.Equal(expected, GatewayAuthorization.HasPermission(role, GatewayCapability.GatewayManage));
    }

    [Fact]
    public void DiagnosticsDoNotExposeCredentials()
    {
        var diagnostic = GatewayDiagnostics.AuthenticationFailed("admin", "secret-password");

        Assert.Equal(GatewayDiagnostics.AuthRequired, diagnostic.Code);
        Assert.DoesNotContain("secret-password", diagnostic.Message);
        Assert.DoesNotContain("admin", diagnostic.Message);
    }
}
