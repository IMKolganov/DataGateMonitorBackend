using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

public class GetAllUsersResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<UserDto> Users { get; set; } = new();
}