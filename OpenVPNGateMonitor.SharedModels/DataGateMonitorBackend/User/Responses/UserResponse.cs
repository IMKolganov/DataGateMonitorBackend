using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

public class UsersResponse
{
    public UserDto User { get; set; } = new();
}