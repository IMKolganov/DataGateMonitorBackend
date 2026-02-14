namespace OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

/// <summary>
/// Результат запроса: уведомление + агрегаты по получателю (IsRead, ReadAt) для одного admin user.
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
