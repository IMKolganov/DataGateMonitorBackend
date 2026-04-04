using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserRoles.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserRoles.Responses;

public class RolesResponse
{
    public List<RoleDto> Roles { get; set; } = [];
}
