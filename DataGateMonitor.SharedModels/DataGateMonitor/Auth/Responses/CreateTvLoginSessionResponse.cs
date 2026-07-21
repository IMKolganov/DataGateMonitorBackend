namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

/// <summary>Payload returned when a TV creates a short-lived login session (QR + user code).</summary>
public sealed class CreateTvLoginSessionResponse
{
    public Guid SessionId { get; set; }

    /// <summary>8-character code, uppercase, hyphenated (e.g. ABCD-1234).</summary>
    public string UserCode { get; set; } = null!;

    /// <summary>Landing page for phone users (without code query).</summary>
    public string VerificationUrl { get; set; } = null!;

    /// <summary>Exact string to encode in the QR code (verification URL with code query).</summary>
    public string QrPayload { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Suggested poll interval for GET /api/auth/tv/session/{sessionId}.</summary>
    public int PollIntervalSeconds { get; set; } = 2;
}
