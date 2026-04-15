namespace DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Dto;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}
