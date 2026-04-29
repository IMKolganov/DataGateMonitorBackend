namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;

/// <summary>Minimal fields for upsert skip logic without loading <c>bytea</c>.</summary>
public sealed record TelegramBotUserProfilePhotoIdMeta(int Id, string? TelegramFileUniqueId);
