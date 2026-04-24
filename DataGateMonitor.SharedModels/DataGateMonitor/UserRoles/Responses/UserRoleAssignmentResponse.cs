using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Responses;

public class UserRoleAssignmentResponse
{
    /// <summary>Null when the user has no role row (unexpected for normal accounts).</summary>
    public UserRoleAssignmentDto? Assignment { get; set; }
}
