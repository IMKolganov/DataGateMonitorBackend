using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;

/// <summary>v2 paged cert-expiry run history (Page/PageSize canon).</summary>
public class GetCertExpiryRunsV2Request
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    public int? VpnServerId { get; set; }
}
