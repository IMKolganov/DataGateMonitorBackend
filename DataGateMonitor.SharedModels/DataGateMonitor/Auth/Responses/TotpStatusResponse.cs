namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class TotpStatusResponse
{
    public bool IsAdmin { get; set; }
    public bool TotpEnabled { get; set; }
    public bool RequiresTotpSetup { get; set; }
}
