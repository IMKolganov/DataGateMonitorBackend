using System.Net.Http.Headers;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.VpnServerPiHoleConfigTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.Api;

public class VpnServerPiHoleConfigService(
    IVpnServerQueryService vpnServerQueryService,
    IVpnServerPiHoleConfigQueryService piHoleConfigQuery,
    IQueryService<VpnServerPiHoleConfig, int> piHoleQuery,
    ICommandService<VpnServerPiHoleConfig, int> piHoleConfigCommand,
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService) : IVpnServerPiHoleConfigService
{
    private const string MaskedPassword = "********";
    private const string AudienceOpenVpnManager = "DataGateOpenVpnManager";

    public async Task<VpnServerPiHoleConfigResponse> GetForAdminAsync(int vpnServerId, CancellationToken ct)
    {
        _ = await vpnServerQueryService.GetById(vpnServerId, ct)
            ?? throw new InvalidOperationException("VPN server not found.");

        var config = await piHoleConfigQuery.GetByVpnServerId(vpnServerId, ct);
        return new VpnServerPiHoleConfigResponse
        {
            Config = ToAdminDto(vpnServerId, config)
        };
    }

    public async Task<VpnServerPiHoleConfigResponse> UpsertAsync(UpsertVpnServerPiHoleConfigRequest request, CancellationToken ct)
    {
        _ = await vpnServerQueryService.GetById(request.VpnServerId, ct)
            ?? throw new InvalidOperationException("VPN server not found.");

        var existing = await piHoleQuery.Query()
            .FirstOrDefaultAsync(x => x.VpnServerId == request.VpnServerId, ct);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var row = new VpnServerPiHoleConfig
            {
                VpnServerId = request.VpnServerId,
                BaseUrl = request.BaseUrl.Trim(),
                AppPassword = request.AppPassword?.Trim() ?? string.Empty,
                PollIntervalSeconds = request.PollIntervalSeconds,
                BatchSize = request.BatchSize,
                LookbackSeconds = request.LookbackSeconds,
                ClientSubnetPrefix = request.ClientSubnetPrefix.Trim(),
                CreateDate = now,
                LastUpdate = now
            };
            await piHoleConfigCommand.Add(row, saveChanges: true, ct);
        }
        else
        {
            existing.BaseUrl = request.BaseUrl.Trim();
            if (!string.IsNullOrWhiteSpace(request.AppPassword))
                existing.AppPassword = request.AppPassword.Trim();
            existing.PollIntervalSeconds = request.PollIntervalSeconds;
            existing.BatchSize = request.BatchSize;
            existing.LookbackSeconds = request.LookbackSeconds;
            existing.ClientSubnetPrefix = request.ClientSubnetPrefix.Trim();
            existing.LastUpdate = now;
            await piHoleConfigCommand.Update(existing, saveChanges: true, ct);
        }

        var saved = await piHoleConfigQuery.GetByVpnServerId(request.VpnServerId, ct);
        return new VpnServerPiHoleConfigResponse
        {
            Config = ToAdminDto(request.VpnServerId, saved)
        };
    }

    public async Task<VpnServerPiHoleRuntimeConfigResponse?> GetRuntimeForMicroserviceAsync(int vpnServerId, CancellationToken ct)
    {
        var server = await vpnServerQueryService.GetById(vpnServerId, ct);
        if (server is null || !server.IsPiHoleEnabled)
            return null;

        var config = await piHoleConfigQuery.GetByVpnServerId(vpnServerId, ct);
        if (config is null)
            return null;

        return new VpnServerPiHoleRuntimeConfigResponse
        {
            VpnServerId = vpnServerId,
            IsPiHoleEnabled = true,
            BaseUrl = config.BaseUrl,
            AppPassword = config.AppPassword,
            PollIntervalSeconds = config.PollIntervalSeconds,
            BatchSize = config.BatchSize,
            LookbackSeconds = config.LookbackSeconds,
            ClientSubnetPrefix = config.ClientSubnetPrefix
        };
    }

    public async Task ApplyRuntimeToMicroserviceAsync(int vpnServerId, CancellationToken ct)
    {
        var server = await vpnServerQueryService.GetById(vpnServerId, ct)
            ?? throw new InvalidOperationException("VPN server not found.");
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API URL is not set for the server.");

        var runtime = await GetRuntimeForMicroserviceAsync(vpnServerId, ct)
            ?? throw new InvalidOperationException("Pi-hole integration is disabled or not configured.");

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceOpenVpnManager));

        var body = new
        {
            enabled = true,
            baseUrl = runtime.BaseUrl,
            appPassword = runtime.AppPassword,
            pollIntervalSeconds = runtime.PollIntervalSeconds,
            batchSize = runtime.BatchSize,
            lookbackSeconds = runtime.LookbackSeconds,
            clientSubnetPrefix = runtime.ClientSubnetPrefix
        };

        using var response = await client.PutAsync(
            "api/pi-hole/config",
            ProjectJson.ToJsonContent(body),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await MicroserviceApiResponseHelper.ReadErrorMessageAsync(response, ct);
            throw new InvalidOperationException(
                $"Failed to apply Pi-hole config to microservice. Status: {(int)response.StatusCode}. Details: {detail}");
        }
    }

    public async Task<PiHoleDiagnosticsResponse> GetMicroserviceDiagnosticsAsync(int vpnServerId, CancellationToken ct)
    {
        var server = await vpnServerQueryService.GetById(vpnServerId, ct)
            ?? throw new InvalidOperationException("VPN server not found.");
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API URL is not set for the server.");

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceOpenVpnManager));

        using var response = await client.GetAsync("api/pi-hole/diagnostics", ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await MicroserviceApiResponseHelper.ReadErrorMessageAsync(response, ct);
            throw new InvalidOperationException(
                $"Failed to read Pi-hole diagnostics from microservice. Status: {(int)response.StatusCode}. Details: {detail}");
        }

        return await MicroserviceApiResponseHelper.ReadSuccessDataAsync<PiHoleDiagnosticsResponse>(response, ct);
    }

    private static VpnServerPiHoleConfigDto ToAdminDto(int vpnServerId, VpnServerPiHoleConfig? config)
    {
        if (config is null)
        {
            return new VpnServerPiHoleConfigDto
            {
                VpnServerId = vpnServerId
            };
        }

        var hasPassword = !string.IsNullOrEmpty(config.AppPassword);
        return new VpnServerPiHoleConfigDto
        {
            VpnServerId = config.VpnServerId,
            BaseUrl = config.BaseUrl,
            AppPassword = hasPassword ? MaskedPassword : string.Empty,
            HasAppPassword = hasPassword,
            PollIntervalSeconds = config.PollIntervalSeconds,
            BatchSize = config.BatchSize,
            LookbackSeconds = config.LookbackSeconds,
            ClientSubnetPrefix = config.ClientSubnetPrefix
        };
    }
}
