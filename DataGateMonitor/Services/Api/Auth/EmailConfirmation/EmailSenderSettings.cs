namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class EmailSenderSettings
{
    public string Provider { get; set; } = "smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string ResendApiKey { get; set; } = string.Empty;
}
