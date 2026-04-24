namespace DataGateMonitor.SharedModels.DataGateXRayManager.Info;

public class ConfigInfoResponse
{
    public string? Dns1 { get; set; }
    public string? Dns2 { get; set; }
    public string? VpnSubnet { get; set; }
    public string? VpnNetmask { get; set; }
    public string? DataDir { get; set; }
    public string? Port { get; set; }
    public string? ApiPort { get; set; }
    public string? Proto { get; set; }
    public XRayManagementInfoResponse XRayManagement { get; set; } = new();
    public string? BackendBaseUrl { get; set; }
}
