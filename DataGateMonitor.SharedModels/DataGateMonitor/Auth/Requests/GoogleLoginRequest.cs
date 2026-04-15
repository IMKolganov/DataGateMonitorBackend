namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

public sealed class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}