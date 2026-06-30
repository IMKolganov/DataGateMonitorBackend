using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

namespace DataGateMonitor.Services.CertExpiry;

public interface ICertExpiryScheduledCheckRunner
{
    Task RunAsync(CancellationToken ct);

    Task<CertExpiryCheckRunResponse> RunCheckAsync(RunCertExpiryCheckRequest request, CancellationToken ct);
}
