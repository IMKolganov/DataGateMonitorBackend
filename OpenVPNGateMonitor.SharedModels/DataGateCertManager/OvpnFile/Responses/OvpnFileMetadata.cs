using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Responses;

public class OvpnFileMetadata
{
    [Required]
    public string CommonName { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public DateTimeOffset IssuedAt { get; set; }

    public string IssuedTo { get; set; } = null!;

    [Required]
    public string CertFilePath { get; set; } = null!;

    [Required]
    public string KeyFilePath { get; set; } = null!;
}