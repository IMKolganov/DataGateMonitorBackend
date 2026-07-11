using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.TelegramBot;

public sealed class TelegramDirectMessageSender(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramChannelSettings> options,
    ILogger<TelegramDirectMessageSender> logger) : ITelegramDirectMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> TrySendMessageAsync(long chatId, string text, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BotToken))
        {
            logger.LogWarning(
                "Telegram bot token is not configured; cannot send direct message to {ChatId}",
                chatId);
            return false;
        }

        if (chatId <= 0 || string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            var url = $"https://api.telegram.org/bot{settings.BotToken}/sendMessage";
            var client = httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                url,
                new TelegramSendMessageRequest { ChatId = chatId, Text = text },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Telegram sendMessage failed for {ChatId}: HTTP {StatusCode}. {ErrorBody}",
                    chatId,
                    (int)response.StatusCode,
                    errorBody);
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(JsonOptions, ct);
            if (payload is not { Ok: true })
            {
                logger.LogWarning(
                    "Telegram sendMessage returned error for {ChatId}: {Description}",
                    chatId,
                    payload?.Description ?? "unknown");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram direct message to {ChatId}", chatId);
            return false;
        }
    }

    private sealed class TelegramSendMessageRequest
    {
        [JsonPropertyName("chat_id")]
        public long ChatId { get; init; }

        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;
    }

    private sealed class TelegramApiResponse
    {
        public bool Ok { get; set; }
        public string? Description { get; set; }
    }
}
