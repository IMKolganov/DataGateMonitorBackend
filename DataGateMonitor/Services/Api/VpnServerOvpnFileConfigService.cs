using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.Enums;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataGateMonitor.Services.Api;

public class VpnServerOvpnFileConfigService(
    ILogger<VpnServerOvpnFileConfigService> logger, 
    IVpnServerOvpnFileConfigQueryService  openVpnServerOvpnFileConfigQueryService,
    ICommandService<VpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService,
    IMicroserviceInfoService microserviceInfoService)
    : IVpnServerOvpnFileConfigService
{
    private readonly ILogger<VpnServerOvpnFileConfigService> _logger = logger;


    public async Task<VpnServerOvpnFileConfig> GetVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(vpnServerId, ct)
               ?? throw new InvalidOperationException("OvpnFileConfig not found");
    }
    
    public async Task<VpnServerOvpnFileConfig> AddOrUpdateVpnServerOvpnFileConfigByServerId(
        VpnServerOvpnFileConfig openVpnServerOvpnFileConfig, bool autoDetectServerSettings, CancellationToken ct)
    {
        if (autoDetectServerSettings)
            await TryApplyDetectedOpenVpnSettingsAsync(openVpnServerOvpnFileConfig, ct);

        var existingConfig = await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(
            openVpnServerOvpnFileConfig.VpnServerId, ct);

        if (existingConfig != null)
        {
            existingConfig.VpnServerIp = openVpnServerOvpnFileConfig.VpnServerIp;
            existingConfig.VpnServerPort = openVpnServerOvpnFileConfig.VpnServerPort;
            existingConfig.ConfigTemplate = openVpnServerOvpnFileConfig.ConfigTemplate;
            existingConfig.LastUpdate = DateTimeOffset.UtcNow;

            await openVpnServerOvpnFileConfigCommandService.Update(existingConfig, true, ct);
        }
        else
        {
            openVpnServerOvpnFileConfig.CreateDate = DateTimeOffset.UtcNow;
            openVpnServerOvpnFileConfig.LastUpdate = DateTimeOffset.UtcNow;
            
            await openVpnServerOvpnFileConfigCommandService.Add(openVpnServerOvpnFileConfig, true, ct);
        }
        
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdId(
                   openVpnServerOvpnFileConfig.VpnServerId, ct)
               ?? throw new InvalidOperationException($"OpenVPN server OVPN file configuration not found for " +
                                                      $"server ID {openVpnServerOvpnFileConfig.VpnServerId}.");
    }

    private async Task TryApplyDetectedOpenVpnSettingsAsync(VpnServerOvpnFileConfig config, CancellationToken ct)
    {
        try
        {
            var diagnostics = await microserviceInfoService.GetInfoAsync(config.VpnServerId, ct);
            if (diagnostics is null || diagnostics.ServerType != VpnServerType.OpenVpn || diagnostics.OpenVpn is null)
                return;

            if (!TryExtractPortProto(diagnostics.OpenVpn, out var port, out var proto))
                return;

            if (port is > 0 and <= 65535)
                config.VpnServerPort = port.Value;

            if (!string.IsNullOrWhiteSpace(proto))
                config.ConfigTemplate = ReplaceProtoDirective(config.ConfigTemplate, proto);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Failed to auto-detect OpenVPN port/proto for VpnServerId={VpnServerId}. Keeping provided values.",
                config.VpnServerId);
        }
    }

    private static bool TryExtractPortProto(object openVpnInfo, out int? port, out string? proto)
    {
        port = null;
        proto = null;

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(openVpnInfo));
        if (!TryGetPropertyIgnoreCase(doc.RootElement, "config", out var cfg) || cfg.ValueKind != JsonValueKind.Object)
            return false;

        if (TryGetPropertyIgnoreCase(cfg, "port", out var portEl) && portEl.TryGetInt32(out var parsedPort))
            port = parsedPort;

        if (TryGetPropertyIgnoreCase(cfg, "proto", out var protoEl) && protoEl.ValueKind == JsonValueKind.String)
        {
            var p = protoEl.GetString()?.Trim().ToLowerInvariant();
            if (p is "tcp" or "udp")
                proto = p;
        }

        return port.HasValue || !string.IsNullOrWhiteSpace(proto);
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string propertyName, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string ReplaceProtoDirective(string template, string proto)
    {
        if (string.IsNullOrWhiteSpace(template))
            return template;

        if (Regex.IsMatch(template, @"^\s*proto\s+\S+", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            return Regex.Replace(template, @"^\s*proto\s+\S+", $"proto {proto}", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return $"proto {proto}\n{template}";
    }
}