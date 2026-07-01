using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;

public interface IVpnServerEventLogQueryService
{
    Task<List<VpnServerEventLog>> GetAll(CancellationToken ct);
    Task<VpnServerEventLog?> GetById(int id, CancellationToken ct);

    Task<IPagedResult<VpnServerEventLog>> GetByVpnServerId(
        int vpnServerId,
        int page,
        int pageSize,
        CancellationToken ct,
        IReadOnlyList<string>? commonNames = null);

    Task<IReadOnlyList<VpnClientAppVersionSummaryItemDto>> GetAppVersionSummaryAsync(
        int vpnServerId,
        IReadOnlyList<string>? commonNames,
        CancellationToken ct);

    Task<IPagedResult<VpnServerEventLog>> GetPage(int page, int pageSize, CancellationToken ct);
}