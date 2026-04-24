using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public class UsersResponse
{
    public UserDto User { get; set; } = new();
}