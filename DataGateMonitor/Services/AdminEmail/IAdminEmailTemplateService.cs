using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

namespace DataGateMonitor.Services.AdminEmail;

public interface IAdminEmailTemplateService
{
    Task<GetEmailTemplatesResponse> ListSummariesAsync(CancellationToken ct);

    Task<EmailBroadcastTemplateDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<EmailBroadcastTemplateDto> CreateAsync(CreateEmailTemplateRequest request, int? createdByUserId,
        CancellationToken ct);

    Task<EmailBroadcastTemplateDto> UpdateAsync(int id, UpdateEmailTemplateRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}
