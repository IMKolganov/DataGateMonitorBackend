namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Dto;

public class ApplicationDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;
}