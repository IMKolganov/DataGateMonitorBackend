using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserRoles.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserRoles.Responses;

public class UserRoleAssignmentResponse
{
    /// <summary>Null when the user has no role row (unexpected for normal accounts).</summary>
    public UserRoleAssignmentDto? Assignment { get; set; }
}
