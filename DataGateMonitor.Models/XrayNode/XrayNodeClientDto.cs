namespace DataGateMonitor.Models.XrayNode;

/// <summary>
/// One active session reported by the DataGate Xray node agent (GET …/api/xray/clients).
/// Field semantics align with <see cref="VpnServerClient"/> / OpenVPN polling.
/// </summary>
public sealed class XrayNodeClientDto
{
    /// <summary>User identity (e.g. VLESS email); maps to <c>CommonName</c> in DB.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Client endpoint, e.g. <c>198.51.100.10:443</c>.</summary>
    public string RemoteAddress { get; set; } = string.Empty;

    /// <summary>Optional display name; defaults to <see cref="Email"/> when saving.</summary>
    public string? Username { get; set; }

    /// <summary>Bytes received from the client at the VPN node (user upload).</summary>
    public long BytesReceived { get; set; }

    /// <summary>Bytes sent to the client from the VPN node (user download).</summary>
    public long BytesSent { get; set; }

    /// <summary>Session start (UTC).</summary>
    public DateTimeOffset ConnectedSince { get; set; }
}
