using Mapster;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTable;

public class VpnServerOverviewQuery(IUnitOfWork uow) : IVpnServerOverviewQuery
{
    // Single roundtrip with correlated subqueries
    public async Task<List<VpnServerWithStatusDto>> GetAllVpnServersWithStatusAsync(
        bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false,
        int? restrictToQuotaPlanId = null,
        CancellationToken ct = default)
    {
        var serversBase = uow.GetQuery<VpnServer>().AsQueryable();
        var servers = includeDeleted ? serversBase : serversBase.Where(s => !s.IsDeleted);
        if (restrictToQuotaPlanId is int pid)
        {
            var allowed = uow.GetQuery<QuotaPlanAllowedServer>().AsQueryable();
            servers = servers.Where(s => allowed.Any(a => a.VpnServerId == s.Id && a.QuotaPlanId == pid));
        }
        else if (requireQuotaPlanAssignment)
        {
            var allowed = uow.GetQuery<QuotaPlanAllowedServer>().AsQueryable();
            servers = servers.Where(s => allowed.Any(a => a.VpnServerId == s.Id));
        }
        var clients = uow.GetQuery<VpnServerClient>().AsQueryable();
        var logs = uow.GetQuery<VpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            orderby s.Id
            select new VpnServerWithStatusDto
            {
                VpnServerResponses = new VpnServerResponse
                {
                    VpnServer = s.Adapt<VpnServerDto>(),
                },

                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),

                VpnServerStatusLogResponse =
                    logs.Where(l => l.VpnServerId == s.Id)
                        .OrderByDescending(l => l.Id) // or by CreateDate
                        .Select(l => new VpnServerStatusLogResponse
                        {
                            VpnServerId = l.VpnServerId,
                            SessionId = l.SessionId,
                            UpSince = l.UpSince,
                            ServerLocalIp = l.ServerLocalIp,
                            ServerRemoteIp = l.ServerRemoteIp,
                            BytesIn = l.BytesIn,
                            BytesOut = l.BytesOut,
                            Version = l.Version
                        })
                        .FirstOrDefault(),

                TotalBytesIn = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn) ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    // Single server variant; throws if not found
    public async Task<VpnServerWithStatusDto> GetVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct)
    {
        var servers = uow.GetQuery<VpnServer>().AsQueryable();
        var clients = uow.GetQuery<VpnServerClient>().AsQueryable();
        var logs = uow.GetQuery<VpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            where s.Id == vpnServerId
            select new VpnServerWithStatusDto
            {
                VpnServerResponses = new VpnServerResponse
                {
                    VpnServer = s.Adapt<VpnServerDto>(),
                },
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),

                VpnServerStatusLogResponse =
                    logs.Where(l => l.VpnServerId == s.Id)
                        .OrderByDescending(l => l.Id)
                        .Select(l => new VpnServerStatusLogResponse
                        {
                            VpnServerId = l.VpnServerId,
                            SessionId = l.SessionId,
                            UpSince = l.UpSince,
                            ServerLocalIp = l.ServerLocalIp,
                            ServerRemoteIp = l.ServerRemoteIp,
                            BytesIn = l.BytesIn,
                            BytesOut = l.BytesOut,
                            Version = l.Version
                        })
                        .FirstOrDefault(),

                TotalBytesIn = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn) ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        var result = await query.AsNoTracking().FirstOrDefaultAsync(ct);
        if (result is null) throw new NullReferenceException("OpenVPN Server not found");
        return result;
    }

    public async Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct)
    {
        var clients = uow.GetQuery<VpnServerClient>().AsQueryable();

        var counters = await clients
            .Where(c => c.VpnServerId == vpnServerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CountConnectedClients = g.Count(c => c.IsConnected),
                CountSessions = g.Count()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return counters is null
            ? (0, 0)
            : (counters.CountConnectedClients, counters.CountSessions);
    }
}