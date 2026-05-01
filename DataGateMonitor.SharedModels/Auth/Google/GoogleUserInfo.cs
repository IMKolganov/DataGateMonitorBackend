namespace DataGateMonitor.SharedModels.Auth.Google;

public sealed class GoogleUserInfo
{
    public string Subject { get; set; } = string.Empty; // "sub" from Google
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? Name { get; set; }
    /// <summary>HTTPS URL from Google ID token <c>picture</c> claim.</summary>
    public string? Picture { get; set; }
}