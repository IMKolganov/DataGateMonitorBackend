namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

public class TotpDisableRequest
{
    public string Code { get; set; } = "";
    public string Password { get; set; } = "";
}
