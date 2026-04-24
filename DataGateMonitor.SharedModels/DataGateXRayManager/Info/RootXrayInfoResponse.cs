namespace DataGateMonitor.SharedModels.DataGateXRayManager.Info;

public class RootXrayInfoResponse
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConfigInfoResponse Config { get; set; } = new();
}
