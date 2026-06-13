using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;

public class GetEmailTemplatesResponse
{
    public List<EmailBroadcastTemplateSummaryDto> Items { get; set; } = new();
}
