using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;

public class CreateOrUpdateQuotaPlanAllowedServerRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Id is required for update.")]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "QuotaPlanId must be greater than 0.")]
    public int QuotaPlanId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "VpnServerId must be greater than 0.")]
    public int VpnServerId { get; set; }
}
