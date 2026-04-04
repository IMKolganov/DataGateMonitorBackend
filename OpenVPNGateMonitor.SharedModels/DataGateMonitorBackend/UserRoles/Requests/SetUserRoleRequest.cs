namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserRoles.Requests;

public class SetUserRoleRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}
