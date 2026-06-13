namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;

public class GetSentEmailHistoryRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
