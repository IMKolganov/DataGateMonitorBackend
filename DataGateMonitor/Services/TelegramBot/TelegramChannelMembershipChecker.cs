using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.TelegramBot.Interfaces;

namespace DataGateMonitor.Services.TelegramBot;

public sealed class TelegramChannelMembershipChecker(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramChannelSettings> options,
    ILogger<TelegramChannelMembershipChecker> logger) : ITelegramChannelMembershipChecker
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> IsSubscribedAsync(long telegramUserId, CancellationToken ct)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BotToken))
        {
            logger.LogWarning(
                "Telegram bot token is not configured; cannot verify channel subscription for user {TelegramUserId}",
                telegramUserId);
            return false;
        }

        if (telegramUserId <= 0)
            return false;

        var chatId = settings.RequiredChannelChatId;
        var url =
            $"https://api.telegram.org/bot{settings.BotToken}/getChatMember?chat_id={Uri.EscapeDataString(chatId)}&user_id={telegramUserId}";

        var client = httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Telegram getChatMember failed for user {TelegramUserId} in {ChatId}: HTTP {StatusCode}",
                telegramUserId,
                chatId,
                (int)response.StatusCode);
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse<TelegramChatMemberPayload>>(JsonOptions, ct);
        if (payload is not { Ok: true, Result: not null })
        {
            logger.LogWarning(
                "Telegram getChatMember returned error for user {TelegramUserId} in {ChatId}: {Description}",
                telegramUserId,
                chatId,
                payload?.Description ?? "unknown");
            return false;
        }

        return IsActiveMemberStatus(payload.Result.Status);
    }

    internal static bool IsActiveMemberStatus(string? status)
        => status?.ToLowerInvariant() switch
        {
            "creator" or "administrator" or "member" or "restricted" => true,
            _ => false,
        };

    private sealed class TelegramApiResponse<T>
    {
        public bool Ok { get; set; }
        public string? Description { get; set; }
        public T? Result { get; set; }
    }

    private sealed class TelegramChatMemberPayload
    {
        public string? Status { get; set; }
    }
}
