namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

public class TotpVerifyLoginRequest
{
    public string LoginChallengeId { get; set; } = "";
    public string Code { get; set; } = "";
}
