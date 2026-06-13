using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public class GetAllUsersResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<UserDto> Users { get; set; } = new();
}