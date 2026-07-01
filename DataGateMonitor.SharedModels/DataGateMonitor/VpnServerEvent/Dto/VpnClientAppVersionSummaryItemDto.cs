namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;

public sealed class VpnClientAppVersionSummaryItemDto
{
    /// <summary>OpenVPN <c>IV_GUI_VER</c> value (client app identifier).</summary>
    public string IvGuiVer { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the latest connect event for this client version.</summary>
    public DateTimeOffset LastConnectedAtUtc { get; set; }

    /// <summary>Number of connect events recorded for this client version.</summary>
    public int ConnectionCount { get; set; }
}
