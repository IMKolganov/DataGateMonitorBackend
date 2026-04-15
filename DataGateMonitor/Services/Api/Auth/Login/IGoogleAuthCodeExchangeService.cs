namespace DataGateMonitor.Services.Api.Auth.Login;

public interface IGoogleAuthCodeExchangeService
{
    Task<string> ExchangeCodeForIdTokenAsync(string code, string codeVerifier, string redirectUri, CancellationToken ct);
}