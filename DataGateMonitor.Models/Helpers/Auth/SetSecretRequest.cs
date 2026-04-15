namespace DataGateMonitor.Models.Helpers.Auth;

public class SetSecretRequest//todo: move to shared models nuget
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}