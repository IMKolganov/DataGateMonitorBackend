using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Auth;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class RoleSeedData
{
    public static readonly Role[] Data =
    {
        new Role
        {
            Id = SystemRoles.AdminId,
            Name = SystemRoles.AdminName,
            NormalizedName = SystemRoles.AdminNormalizedName,
            Description = "System administrator",
            IsSystem = true
        },
        new Role
        {
            Id = SystemRoles.VpnUserId,
            Name = SystemRoles.VpnUserName,
            NormalizedName = SystemRoles.VpnUserNormalizedName,
            Description = "Regular VPN user",
            IsSystem = true
        },
        new Role
        {
            Id = SystemRoles.ServiceId,
            Name = SystemRoles.ServiceName,
            NormalizedName = SystemRoles.ServiceNormalizedName,
            Description = "Internal service account",
            IsSystem = true
        },
    };
}
