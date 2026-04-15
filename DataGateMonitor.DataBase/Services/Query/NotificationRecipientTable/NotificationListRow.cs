namespace DataGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

/// <summary>
/// Query result: notification plus recipient aggregates (IsRead, ReadAt) for one admin user.
/// </summary>
public record NotificationListRow(
    int Id,
    string Type,
    int Severity,
    string Title,
    string? Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
