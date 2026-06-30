namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

public class GetCertExpiryRunsResponse
{
    public List<CertExpiryRunSummaryDto> Runs { get; set; } = [];
}
