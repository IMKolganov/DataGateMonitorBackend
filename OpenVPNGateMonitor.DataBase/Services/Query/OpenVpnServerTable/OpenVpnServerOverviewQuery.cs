using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerOverviewQuery(IUnitOfWork uow) : IOpenVpnServerOverviewQuery
{
    // Reusable DB-side projection to DTO
    private static readonly Expression<Func<OpenVpnServerClient, VpnClientInfoResponse>> VpnClientSelect =
        x => new VpnClientInfoResponse
        {
            Id = x.Id,
            VpnServerId = x.VpnServerId,
            ExternalId = x.ExternalId,
            SessionId = x.SessionId,
            CommonName = x.CommonName,
            RemoteIp = x.RemoteIp,
            LocalIp = x.LocalIp,
            BytesReceived = x.BytesReceived,
            BytesSent = x.BytesSent,
            ConnectedSince = x.ConnectedSince,
            Username = x.Username,
            Country = x.Country,
            Region = x.Region,
            City = x.City,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            IsConnected = x.IsConnected,
            // Telegram fields are enriched later:
            TgUsername = null,
            TgFirstName = null,
            TgLastName = null
        };

    // Single roundtrip with correlated subqueries
    public async Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatus(CancellationToken ct)
    {
        var servers = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs    = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            orderby s.Id
            select new OpenVpnServerWithStatus
            {
                OpenVpnServer = s,
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),
                OpenVpnServerStatusLog = logs
                    .Where(l => l.VpnServerId == s.Id)
                    .OrderByDescending(l => l.Id) // or by CreateDate
                    .FirstOrDefault(),
                TotalBytesIn  = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn)  ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    // Single server variant; throws if not found
    public async Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatus(int vpnServerId, CancellationToken ct)
    {
        var servers = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs    = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            where s.Id == vpnServerId
            select new OpenVpnServerWithStatus
            {
                OpenVpnServer = s,
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),
                OpenVpnServerStatusLog = logs
                    .Where(l => l.VpnServerId == s.Id)
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefault(),
                TotalBytesIn  = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn)  ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        var result = await query.AsNoTracking().FirstOrDefaultAsync(ct);
        if (result is null) throw new NullReferenceException("OpenVPN Server not found");
        return result;
    }

    // Connected clients page + Telegram enrichment
    public async Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var q = uow.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId && x.IsConnected);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(VpnClientSelect)
            .AsNoTracking()
            .ToListAsync(ct);

        await EnrichWithTelegramAsync(items, ct);

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = items,
            TotalCount = total
        };
    }

    // Full history page + Telegram enrichment
    public async Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var q = uow.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(VpnClientSelect)
            .AsNoTracking()
            .ToListAsync(ct);

        await EnrichWithTelegramAsync(items, ct);

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = items,
            TotalCount = total
        };
    }

    // Helper: enrich page items with Telegram user fields by ExternalId->TelegramId
    private async Task EnrichWithTelegramAsync(IEnumerable<VpnClientInfoResponse> items, CancellationToken ct)
    {
        var externalIds = items
            .Select(c => long.TryParse(c.ExternalId, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (externalIds.Count == 0) return;

        var tgDict = await uow.GetQuery<TelegramBotUser>()
            .AsQueryable()
            .Where(x => externalIds.Contains(x.TelegramId))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.TelegramId, ct);

        foreach (var c in items)
        {
            if (!long.TryParse(c.ExternalId, out var ext)) continue;
            if (tgDict.TryGetValue(ext, out var u))
            {
                c.TgUsername  = u.Username;
                c.TgFirstName = u.FirstName;
                c.TgLastName  = u.LastName;
            }
        }
    }
}
