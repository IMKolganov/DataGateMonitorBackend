namespace DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Dto;

public class UserRoleAssignmentDto
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
}
