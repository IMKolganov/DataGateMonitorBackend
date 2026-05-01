using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;

namespace DataGateMonitor.Services.AdminEmail;

public interface IAdminEmailBroadcastService
{
    Task<GetSentEmailHistoryResponse> GetHistoryAsync(GetSentEmailHistoryRequest request, CancellationToken ct);

    Task<SendAdminEmailResponse> SendAsync(SendAdminEmailRequest request, int? sentByUserId, CancellationToken ct);
}
