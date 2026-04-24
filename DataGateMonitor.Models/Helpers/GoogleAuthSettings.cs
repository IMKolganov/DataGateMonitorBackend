namespace DataGateMonitor.Models.Helpers;

public sealed class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty; // optional
}