namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class TotpSetupResponse
{
    public string SharedSecret { get; set; } = "";
    public string OtpAuthUri { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string AccountName { get; set; } = "";
}
