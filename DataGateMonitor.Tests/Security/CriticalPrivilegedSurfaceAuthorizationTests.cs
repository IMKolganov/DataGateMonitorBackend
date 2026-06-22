using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Controllers;
using DataGateMonitor.Hubs;
using Xunit;

namespace DataGateMonitor.Tests.Security;

/// <summary>
/// Regression guards for admin-only / management surfaces.
/// OpenVpnFrontendHub historically had no [Authorize]: any valid JWT (including VpnUser) could
/// connect via <c>wss://…/api/hubs/frontend?access_token=…</c> and proxy OpenVPN management commands.
/// </summary>
public sealed class CriticalPrivilegedSurfaceAuthorizationTests
{
    private static readonly string[] AdminAppRoles = ["Admin", "App"];

    public static TheoryData<Type, string[]> AdminOnlyControllers { get; } = new()
    {
        { typeof(VpnServerCertsController), AdminAppRoles },
        { typeof(VpnServerOvpnFileConfigController), AdminAppRoles },
        { typeof(VpnServerEventController), AdminAppRoles },
    };

    public static TheoryData<Type, string[]> AdminOnlyPiHoleControllers { get; } = new()
    {
        { typeof(VpnDnsQueryController), ["Admin"] },
    };

    private static readonly string[] PiHoleConfigAdminMethods =
    [
        nameof(VpnServerPiHoleConfigController.Get),
        nameof(VpnServerPiHoleConfigController.Upsert),
        nameof(VpnServerPiHoleConfigController.ApplyRuntime),
        nameof(VpnServerPiHoleConfigController.GetDiagnostics),
    ];

    [Theory]
    [MemberData(nameof(AdminOnlyPiHoleControllers))]
    public void PiHoleSurface_RequiresAdminOnly_NotVpnUserOrApp(Type controllerType, string[] expectedRoles)
    {
        var roles = GetDeclaredRoleNames(controllerType);
        Assert.NotEmpty(roles);
        Assert.All(expectedRoles, r => Assert.Contains(r, roles));
        Assert.DoesNotContain("VpnUser", roles);
        Assert.DoesNotContain("App", roles);
    }

    [Fact]
    public void VpnServerPiHoleConfig_AdminEndpoints_RequireAdminOnly()
    {
        foreach (var methodName in PiHoleConfigAdminMethods)
        {
            var method = typeof(VpnServerPiHoleConfigController).GetMethod(methodName);
            Assert.NotNull(method);
            var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorize);
            Assert.Equal("Admin", authorize!.Roles);
        }
    }

    [Fact]
    public void VpnServerPiHoleConfig_GetRuntime_RequiresAppRoleOnly()
    {
        var method = typeof(VpnServerPiHoleConfigController).GetMethod(nameof(VpnServerPiHoleConfigController.GetRuntime));
        Assert.NotNull(method);
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal("App", authorize!.Roles);
    }

    [Theory]
    [MemberData(nameof(AdminOnlyControllers))]
    public void Controller_RequiresAdminOrAppRoles_NotVpnUser(Type controllerType, string[] expectedRoles)
    {
        var roles = GetDeclaredRoleNames(controllerType);
        Assert.NotEmpty(roles);
        Assert.All(expectedRoles, r => Assert.Contains(r, roles));
        Assert.DoesNotContain("VpnUser", roles);
    }

    [Fact]
    public void OpenVpnFrontendHub_RequiresAdminOrAppRoles_NotVpnUser()
    {
        var roles = GetDeclaredRoleNames(typeof(OpenVpnFrontendHub));
        Assert.Contains("Admin", roles);
        Assert.Contains("App", roles);
        Assert.DoesNotContain("VpnUser", roles);
    }

    [Fact]
    public void OpenVpnFrontendHub_HasAuthorizeBeforeConnect()
    {
        Assert.True(HasAuthorizeAttribute(typeof(OpenVpnFrontendHub)));
    }

    /// <summary>
    /// Dashboard-facing hubs must require authentication (JWT via access_token query is still authenticated).
    /// </summary>
    [Theory]
    [InlineData(typeof(OpenVpnFrontendHub))]
    [InlineData(typeof(OpenVpnProxyTrafficFlowHub))]
    [InlineData(typeof(OpenVpnStatusHub))]
    [InlineData(typeof(GeoLiteHub))]
    public void DashboardHub_RequiresAuthentication(Type hubType)
    {
        Assert.True(HasAuthorizeAttribute(hubType), $"Hub {hubType.Name} must have [Authorize].");
    }

  private static bool HasAuthorizeAttribute(Type type) =>
        type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();

    private static HashSet<string> GetDeclaredRoleNames(Type type)
    {
        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in type.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
        {
            if (string.IsNullOrWhiteSpace(attr.Roles))
                continue;

            foreach (var part in attr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                roles.Add(part);
        }

        return roles;
    }
}
