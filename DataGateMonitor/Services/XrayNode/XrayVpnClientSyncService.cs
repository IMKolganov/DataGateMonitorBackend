using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Models.XrayNode;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.Services.Helpers.Interfaces;

namespace DataGateMonitor.Services.XrayNode;

public sealed class XrayVpnClientSyncService(
    ILogger<XrayVpnClientSyncService> logger,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IUserQueryService userQueryService,
    IGeoLiteQueryService geoLiteQueryService,
    ITransactionRunner transactionRunner,
    ICommandService<VpnServerClient, int> vpnServerClientCommandService,
    ICommandService<VpnServerClientTraffic, int> vpnClientTrafficCommandService)
    : IXrayVpnClientSyncService
{
    private const string UnknownLocalIpPlaceholder = "-";

    public async Task SyncConnectedClientsAsync(VpnServer server, IReadOnlyList<XrayNodeClientDto> clients,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("VpnServerId: {Id}. Xray SyncConnectedClientsAsync: {Count} client(s).",
            server.Id, clients.Count);

        await transactionRunner.RunAsync(async _ =>
        {
            var nowUtc = DateTimeOffset.UtcNow;

            var currentSessionIds = new HashSet<Guid>(
                clients.Select(c =>
                    VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(
                        c.Email, c.RemoteAddress, c.ConnectedSince)));

            await vpnServerClientCommandService.UpdateWhere(
                x => x.VpnServerId == server.Id
                     && x.IsConnected
                     && !currentSessionIds.Contains(x.SessionId),
                s => s
                    .SetProperty(c => c.IsConnected, false)
                    .SetProperty(c => c.DisconnectedAt, nowUtc)
                    .SetProperty(c => c.LastUpdate, nowUtc),
                cancellationToken);

            foreach (var c in clients)
            {
                var sessionId = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(
                    c.Email, c.RemoteAddress, c.ConnectedSince);

                var commonName = c.Email;
                var externalId = await issuedOvpnFileQueryService.GetExternalIdByCommonName(
                    commonName, server.Id, cancellationToken) ?? string.Empty;
                var user = await userQueryService.GetByExternalId(externalId, cancellationToken);

                var username = string.IsNullOrWhiteSpace(c.Username) ? commonName : c.Username!;

                var (country, region, city, lat, lon) = await ResolveGeoAsync(c.RemoteAddress, cancellationToken);

                var rows = await vpnServerClientCommandService.UpdateWhere(
                    x => x.VpnServerId == server.Id && x.SessionId == sessionId,
                    s => s
                        .SetProperty(x => x.UserId, user?.Id)
                        .SetProperty(x => x.CommonName, commonName)
                        .SetProperty(x => x.RemoteIp, c.RemoteAddress)
                        .SetProperty(x => x.ProxyRealIp, (string?)null)
                        .SetProperty(x => x.LocalIp, UnknownLocalIpPlaceholder)
                        .SetProperty(x => x.BytesReceived, c.BytesReceived)
                        .SetProperty(x => x.BytesSent, c.BytesSent)
                        .SetProperty(x => x.ConnectedSince, c.ConnectedSince)
                        .SetProperty(x => x.Username, username)
                        .SetProperty(x => x.Country, country)
                        .SetProperty(x => x.Region, region)
                        .SetProperty(x => x.City, city)
                        .SetProperty(x => x.Latitude, lat)
                        .SetProperty(x => x.Longitude, lon)
                        .SetProperty(x => x.ExternalId, externalId)
                        .SetProperty(x => x.IsConnected, true)
                        .SetProperty(x => x.DisconnectedAt, _ => (DateTimeOffset?)null)
                        .SetProperty(x => x.LastUpdate, nowUtc),
                    cancellationToken);

                if (rows == 0)
                {
                    var newClient = new VpnServerClient
                    {
                        VpnServerId = server.Id,
                        UserId = user?.Id,
                        SessionId = sessionId,
                        CommonName = commonName,
                        RemoteIp = c.RemoteAddress,
                        ProxyRealIp = null,
                        LocalIp = UnknownLocalIpPlaceholder,
                        BytesReceived = c.BytesReceived,
                        BytesSent = c.BytesSent,
                        ConnectedSince = c.ConnectedSince,
                        Username = username,
                        Country = country,
                        Region = region,
                        City = city,
                        Latitude = lat,
                        Longitude = lon,
                        ExternalId = externalId,
                        IsConnected = true,
                        DisconnectedAt = null,
                        LastUpdate = nowUtc,
                        CreateDate = nowUtc,
                    };

                    await vpnServerClientCommandService.Add(newClient, saveChanges: false, cancellationToken);
                }

                var measuredAt = DateTimeOffset.UtcNow;

                var tRows = await vpnClientTrafficCommandService.UpdateWhere(
                    x => x.VpnServerId == server.Id
                         && x.SessionId == sessionId
                         && x.MeasuredAt == measuredAt
                         && x.BytesReceived <= c.BytesReceived
                         && x.BytesSent <= c.BytesSent,
                    s => s
                        .SetProperty(t => t.ExternalId, externalId)
                        .SetProperty(t => t.BytesReceived, c.BytesReceived)
                        .SetProperty(t => t.BytesSent, c.BytesSent),
                    cancellationToken);

                if (tRows == 0)
                {
                    var sample = new VpnServerClientTraffic
                    {
                        VpnServerId = server.Id,
                        UserId = user?.Id,
                        ExternalId = externalId,
                        SessionId = sessionId,
                        BytesReceived = c.BytesReceived,
                        BytesSent = c.BytesSent,
                        MeasuredAt = measuredAt,
                    };

                    await vpnClientTrafficCommandService.Add(sample, saveChanges: false, cancellationToken);
                }
            }

            await vpnServerClientCommandService.SaveChanges(cancellationToken);
            await vpnClientTrafficCommandService.SaveChanges(cancellationToken);

            logger.LogInformation("VpnServerId: {Id}. Xray SyncConnectedClientsAsync completed.", server.Id);
        }, cancellationToken);
    }

    private async Task<(string? Country, string? Region, string? City, double? Latitude, double? Longitude)>
        ResolveGeoAsync(string remoteAddress, CancellationToken cancellationToken)
    {
        var host = ClientEndpointHost.TryGetHostForGeoLookup(remoteAddress);
        if (host is null || !ClientEndpointHost.IsNonLoopbackIpOrHost(host))
            return (null, null, null, null, null);

        try
        {
            var geo = await geoLiteQueryService.GetGeoInfoAsync(host, cancellationToken);
            if (geo is null)
                return (null, null, null, null, null);

            return (geo.Country, geo.Region, geo.City, geo.Latitude, geo.Longitude);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "GeoLite lookup failed for endpoint host {Host}", host);
            return (null, null, null, null, null);
        }
    }
}
