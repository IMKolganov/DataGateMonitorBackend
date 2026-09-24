using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

/// <summary>v2 paged cert-expiry run history.</summary>
public class GetCertExpiryRunsV2Response
{
    public PagedResponse<CertExpiryRunSummaryDto> Runs { get; set; } = new();
}
