using DataGateMonitor.SharedModels.DataGateMonitor.User.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public sealed class GetUserPasswordHistoryResponse
{
    public List<UserPasswordHistoryItemDto> Items { get; set; } = [];
}
