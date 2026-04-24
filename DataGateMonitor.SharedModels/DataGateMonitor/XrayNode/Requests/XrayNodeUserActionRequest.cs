using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.XrayNode.Requests;

public sealed class XrayNodeUserActionRequest
{
    [Required]
    public string CommonName { get; set; } = string.Empty;
}
