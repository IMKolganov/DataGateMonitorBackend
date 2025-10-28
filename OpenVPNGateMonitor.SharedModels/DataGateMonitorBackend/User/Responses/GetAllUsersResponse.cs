using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

public class GetAllUsersResponse
{
    public List<UserDto> Users { get; set; } = new();
}