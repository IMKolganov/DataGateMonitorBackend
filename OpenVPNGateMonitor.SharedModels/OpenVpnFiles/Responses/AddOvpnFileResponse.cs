using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;

public class AddOvpnFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new IssuedOvpnFileDto();
}