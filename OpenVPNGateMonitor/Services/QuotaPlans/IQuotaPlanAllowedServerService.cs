using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;

namespace OpenVPNGateMonitor.Services.QuotaPlans;

public interface IQuotaPlanAllowedServerService
{
    Task<GetAllQuotaPlanAllowedServersResponse> GetPageAsync(GetAllQuotaPlanAllowedServersRequest request, CancellationToken ct);
    Task<QuotaPlanAllowedServerResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<QuotaPlanAllowedServerDto>> GetListByQuotaPlanIdAsync(int quotaPlanId, CancellationToken ct);
    Task<List<QuotaPlanAllowedServerDto>> GetListByVpnServerIdAsync(int vpnServerId, CancellationToken ct);
    Task<QuotaPlanAllowedServerResponse> CreateAsync(CreateOrUpdateQuotaPlanAllowedServerRequest request, CancellationToken ct);
    Task UpdateAsync(CreateOrUpdateQuotaPlanAllowedServerRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
