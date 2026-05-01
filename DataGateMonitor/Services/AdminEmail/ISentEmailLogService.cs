namespace DataGateMonitor.Services.AdminEmail;

/// <summary>Appends a row to SentEmailLogs for any outbound transactional email.</summary>
public interface ISentEmailLogService
{
    Task LogAsync(
        int? recipientUserId,
        string recipientEmail,
        string subject,
        string bodyHtml,
        bool success,
        string? errorMessage,
        int? sentByUserId,
        CancellationToken ct);
}
