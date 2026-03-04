using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

public class OvpnFileDownload
{
    [Required]
    public string CommonName { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public byte[]? Content { get; set; }
}