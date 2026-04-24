namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

public class IssuedOvpnFileWithTokenDto
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new();
    public IssuedOvpnFileTokenDto IssuedOvpnFileToken { get; set; } = new();
}