using Newtonsoft.Json;

namespace DataGateMonitor.Services.Api.Auth.Login;

public sealed class GoogleAuthCodeExchangeService(HttpClient http, IConfiguration configuration)
    : IGoogleAuthCodeExchangeService
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly string _clientId = configuration["GoogleAuth:DesktopClientId"]
                                        ?? throw new InvalidOperationException("GoogleAuth:DesktopClientId is not configured.");
    private readonly string _clientSecret = configuration["GoogleAuth:DesktopClientSecret"]
                                            ?? throw new InvalidOperationException("GoogleAuth:DesktopClientSecret is not configured.");

    public async Task<string> ExchangeCodeForIdTokenAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(codeVerifier))
            throw new ArgumentException("CodeVerifier is required.", nameof(codeVerifier));

        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("RedirectUri is required.", nameof(redirectUri));

        using var form = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("client_id", _clientId),
            new("client_secret", _clientSecret),
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri),
            new("code_verifier", codeVerifier),
        });

        using var resp = await _http.PostAsync("https://oauth2.googleapis.com/token", form, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Google token exchange failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {json}");

        var tr = JsonConvert.DeserializeObject<TokenResponse>(json);
        if (tr == null || string.IsNullOrWhiteSpace(tr.IdToken))
            throw new InvalidOperationException("Google token exchange did not return id_token.");

        return tr.IdToken!;
    }

    private sealed class TokenResponse
    {
        [JsonProperty("id_token")]
        public string? IdToken { get; set; }
    }
}