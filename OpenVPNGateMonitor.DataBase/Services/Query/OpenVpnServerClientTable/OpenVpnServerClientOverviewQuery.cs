using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OpenVpnServerClientOverviewQuery(
    IUnitOfWork uow, IUserQueryService userQueryService) : IOpenVpnServerClientOverviewQuery
{
    // Reusable DB-side projection to DTO
    private static readonly Expression<Func<OpenVpnServerClient, VpnClientInfoDto>> VpnClientSelect =
        x => new VpnClientInfoDto
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
            DisplayName = string.Empty,
        };

    // Connected clients page
    public async Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClientsAsync(
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

        await EnrichWithUsersAsync(items, ct);

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = items,
            TotalCount = total
        };
    }

    // Full history page + Telegram enrichment
    public async Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClientsAsync(
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

        await EnrichWithUsersAsync(items, ct);

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = items,
            TotalCount = total
        };
    }

    // Helper: enrich page items with User.DisplayName by ExternalId
    private async Task EnrichWithUsersAsync(List<VpnClientInfoDto> items, CancellationToken ct)
    {
        var externalIds = items
            .Select(c => c.ExternalId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (externalIds.Count == 0)
            return;

        var users = new Dictionary<string, User?>();
        foreach (var extId in externalIds)
        {
            var user = await userQueryService.GetByExternalId(extId, ct);
            users[extId] = user;
        }

        foreach (var client in items)
        {
            if (string.IsNullOrWhiteSpace(client.ExternalId))
                continue;

            if (users.TryGetValue(client.ExternalId, out var user) && user is not null)
                client.DisplayName = user.DisplayName;
        }
    }
}