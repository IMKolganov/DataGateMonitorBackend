using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class DownloadFileResponse
{
    public IssuedOvpnFileDto IssuedOvpn { get; set; } = new();
    public long FileSizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}