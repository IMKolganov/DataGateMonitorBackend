using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

public class DownloadFileResponse
{
    public IssuedOvpnFileDto IssuedOvpn { get; set; } = new();
    public long FileSizeBytes { get; set; }
    public byte[]? Content { get; set; }
}