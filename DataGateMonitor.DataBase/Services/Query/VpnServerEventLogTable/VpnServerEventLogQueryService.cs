using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;

public class VpnServerEventLogQueryService(IQueryService<VpnServerEventLog, int> q)
    : IVpnServerEventLogQueryService
{
    private static readonly string[] ConnectEventTypes = ["ClientConnected", "ClientConnect"];

    public Task<List<VpnServerEventLog>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<VpnServerEventLog?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public async Task<IPagedResult<VpnServerEventLog>> GetByVpnServerId(
        int vpnServerId,
        int page,
        int pageSize,
        CancellationToken ct,
        IReadOnlyList<string>? commonNames = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = q.Query()
            .Where(x => x.VpnServerId == vpnServerId);

        if (commonNames is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x =>
                x.CommonName != null && commonNames.Contains(x.CommonName));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<VpnServerEventLog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<IReadOnlyList<VpnClientAppVersionSummaryItemDto>> GetAppVersionSummaryAsync(
        int vpnServerId,
        IReadOnlyList<string>? commonNames,
        CancellationToken ct)
    {
        var baseQuery = q.Query()
            .Where(x => x.VpnServerId == vpnServerId)
            .Where(x => ConnectEventTypes.Contains(x.EventType))
            .Where(x =>
                (x.IvGuiVer != null && x.IvGuiVer != "")
                || (x.IvVer != null && x.IvVer != ""));

        if (commonNames is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x =>
                x.CommonName != null && commonNames.Contains(x.CommonName));
        }

        return await baseQuery
            .GroupBy(x =>
                x.IvGuiVer != null && x.IvGuiVer != ""
                    ? x.IvGuiVer
                    : x.IvVer!)
            .Select(g => new VpnClientAppVersionSummaryItemDto
            {
                IvGuiVer = g.Key!,
                LastConnectedAtUtc = g.Max(x => x.EventTimeUtc ?? x.CreateDate),
                ConnectionCount = g.Count(),
            })
            .OrderByDescending(x => x.LastConnectedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IPagedResult<VpnServerEventLog>> GetPage(int page, int pageSize, CancellationToken ct)
        => await q.Page(page, pageSize, ct: ct);
}