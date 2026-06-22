using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;

public interface IVpnDnsQueryLogQueryService
{
    Task<IPagedResult<VpnDnsQueryLog>> SearchAsync(GetVpnDnsQueryRequest request, CancellationToken ct);
}
