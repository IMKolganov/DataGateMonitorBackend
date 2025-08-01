namespace OpenVPNGateMonitor.Models.Test;

public class ActiveClientResponse
{
    public string ExternalId { get; set; } = null!;
    public DateTime LastConnection { get; set; }
    public string CommonName { get; set; } = null!;
    public string RemoteIp { get; set; } = null!;
    public string? TgUsername { get; set; }
    public string? TgFirstName { get; set; }
    public string? TgLastName { get; set; }
}
