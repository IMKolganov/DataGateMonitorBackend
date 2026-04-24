using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

/// <summary>
/// Fetches microservice INFO (api/info) and appends to conflog only when payload changed.
/// </summary>
public interface IVpnServerConflogService
{
    /// <summary>
    /// Request INFO by base URL; if payload differs from last record for this URL, saves and returns the new entity; otherwise returns null.
    /// </summary>
    /// <param name="baseUrl">Base URL of the microservice (e.g. https://host:port/).</param>
    /// <param name="vpnServerId">Optional server id when request is tied to a known server.</param>
    Task<VpnServerConflog?> FetchAndSaveIfChangedAsync(string baseUrl, int? vpnServerId, CancellationToken ct = default);

    /// <summary>
    /// Request INFO by server id (uses server ApiUrl); saves to conflog only when payload changed.
    /// </summary>
    Task<VpnServerConflog?> FetchAndSaveIfChangedByServerIdAsync(int vpnServerId, CancellationToken ct = default);
}
