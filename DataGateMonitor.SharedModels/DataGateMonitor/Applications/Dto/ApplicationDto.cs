namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Dto;

public class ApplicationDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRevoked { get; set; } = false;
    public bool IsSystem { get; set; } = false;
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}