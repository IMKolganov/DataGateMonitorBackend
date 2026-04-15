using System.Text.Json;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

public class VpnServerConflogService(
    IMicroserviceInfoService microserviceInfoService,
    IVpnServerConflogQueryService conflogQueryService,
    ICommandService<VpnServerConflog, int> conflogCommandService,
    IVpnServerQueryService openVpnServerQueryService) : IVpnServerConflogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<VpnServerConflog?> FetchAndSaveIfChangedAsync(string baseUrl, int? vpnServerId, CancellationToken ct = default)
    {
        var response = await microserviceInfoService.GetInfoByUrlAsync(baseUrl, ct);
        if (response is null)
            return null;
        var payloadJson = JsonSerializer.Serialize(response, JsonOptions);
        var requestUrl = baseUrl.TrimEnd('/').Trim();

        // When called by server id: only compare with last record for THIS server (by VpnServerId).
        // Do not fallback to GetLastByRequestUrl — after server recreate the same URL may have old
        // conflog from the deleted server, so we would skip saving and the new server would never get history.
        VpnServerConflog? last = null;
        if (vpnServerId.HasValue)
            last = await conflogQueryService.GetLastByVpnServerId(vpnServerId.Value, ct);
        if (last == null && !vpnServerId.HasValue)
            last = await conflogQueryService.GetLastByRequestUrl(requestUrl, ct);

        if (last != null && last.PayloadJson == payloadJson)
            return null;

        var entity = new VpnServerConflog
        {
            VpnServerId = vpnServerId,
            RequestUrl = requestUrl,
            PayloadJson = payloadJson
        };
        await conflogCommandService.Add(entity, saveChanges: true, ct);
        return entity;
    }

    public async Task<VpnServerConflog?> FetchAndSaveIfChangedByServerIdAsync(int vpnServerId, CancellationToken ct = default)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, ct)
                     ?? throw new InvalidOperationException($"OpenVPN server not found: {vpnServerId}");
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("Server ApiUrl is not set.");
        return await FetchAndSaveIfChangedAsync(server.ApiUrl, vpnServerId, ct);
    }
}
