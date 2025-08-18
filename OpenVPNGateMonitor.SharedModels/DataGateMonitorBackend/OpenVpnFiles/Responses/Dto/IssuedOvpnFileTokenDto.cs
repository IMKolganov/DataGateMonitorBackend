namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

public class IssuedOvpnFileTokenDto
{
    public int Id { get; set; }
    public int IssuedOvpnFileId { get; set; }
    public string Token { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public string? Purpose { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}