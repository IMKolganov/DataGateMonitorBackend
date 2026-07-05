using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Requests;

public class GetVpnServerEventRequest
{
    [Required(ErrorMessage = "vpnServerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "vpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "pageSize must be greater than 0.")]
    public int PageSize { get; set; } = 10;

    /// <summary>Optional filter: single OpenVPN common name (CN).</summary>
    public string? CommonName { get; set; }

    /// <summary>Optional filter: resolve all issued profile CNs for this external id on the server.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional filter: exact event type (e.g. CONNECT, DISCONNECT).</summary>
    public string? EventType { get; set; }
}