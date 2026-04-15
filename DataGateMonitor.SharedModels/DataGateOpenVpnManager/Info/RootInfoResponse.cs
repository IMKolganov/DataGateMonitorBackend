namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

public class RootInfoResponse
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConfigInfoResponse? Config { get; set; }
}
