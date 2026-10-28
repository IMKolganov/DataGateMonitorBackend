namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Requests;

public sealed class LocalProfileCreateRequest
{
    public string CommonName { get; set; } = string.Empty;
    public bool IsServiceProfile { get; set; }

    /// <summary>Host clients dial; defaults to PUBLIC_IP.</summary>
    public string? Remote { get; set; }

    /// <summary>Defaults to the node's OpenVPN PORT.</summary>
    public int? Port { get; set; }

    public int? ExpireDays { get; set; }
}
