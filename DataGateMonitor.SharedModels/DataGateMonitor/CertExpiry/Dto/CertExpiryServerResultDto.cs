using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;

public class CertExpiryServerResultDto
{
    public int VpnServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public CertExpiryServerFetchStatus FetchStatus { get; set; }
    public string? FetchError { get; set; }
    public long DurationMs { get; set; }
    public List<CertExpiryProfileResultDto> Profiles { get; set; } = [];
}
