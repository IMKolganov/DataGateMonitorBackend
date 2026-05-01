using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.AdminEmail;

public sealed class SentEmailLogService(ICommandService<SentEmailLog, int> command) : ISentEmailLogService
{
    private const int MaxErrorLen = 4000;

    public async Task LogAsync(
        int? recipientUserId,
        string recipientEmail,
        string subject,
        string bodyHtml,
        bool success,
        string? errorMessage,
        int? sentByUserId,
        CancellationToken ct)
    {
        var email = (recipientEmail ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(email))
            return;

        var now = DateTimeOffset.UtcNow;
        var err = errorMessage;
        if (err != null && err.Length > MaxErrorLen)
            err = err[..MaxErrorLen];

        var log = new SentEmailLog
        {
            RecipientUserId = recipientUserId,
            RecipientEmail = email,
            Subject = (subject ?? string.Empty).Trim().Length > 0 ? subject!.Trim() : "(no subject)",
            BodyHtml = bodyHtml ?? string.Empty,
            Success = success,
            ErrorMessage = err,
            SentByUserId = sentByUserId,
            CreateDate = now,
            LastUpdate = now
        };

        await command.Add(log, saveChanges: true, ct);
    }
}
