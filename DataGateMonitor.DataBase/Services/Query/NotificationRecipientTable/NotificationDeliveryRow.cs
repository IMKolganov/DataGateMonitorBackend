using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

public record NotificationDeliveryRow(
    int NotificationId,
    string Channel,
    DeliveryStatus Status,
    string? Error,
    DateTimeOffset? DeliveredAt);
