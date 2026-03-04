namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;

public class RegisterApplicationResponse
{
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } =  string.Empty;
    public string ClientSecret { get; set; } =  string.Empty;
    public bool IsRevoked { get; set; } = false;
    public bool IsSystem { get; set; } = false;
}