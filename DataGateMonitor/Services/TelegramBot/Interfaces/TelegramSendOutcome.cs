namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public sealed record TelegramSendOutcome(bool Success, string? ErrorMessage)
{
    public static TelegramSendOutcome Ok() => new(true, null);
    public static TelegramSendOutcome Fail(string error) => new(false, error);
}
