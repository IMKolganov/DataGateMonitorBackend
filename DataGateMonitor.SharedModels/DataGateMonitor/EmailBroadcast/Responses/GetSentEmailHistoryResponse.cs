using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;

public class GetSentEmailHistoryResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<SentEmailLogDto> Items { get; set; } = new();
}
