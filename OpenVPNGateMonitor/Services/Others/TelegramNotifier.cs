using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

namespace OpenVPNGateMonitor.Services.Others;

public class TelegramNotifier(
    ITelegramUserService telegramUserService,
    ILogger<TelegramNotifier> logger)
    : INotifier
{
    public string Channel => "telegram";

    private readonly ITelegramUserService _telegramUserService = telegramUserService;
    private readonly ILogger<TelegramNotifier> _logger = logger;

    public Task SendAsync(Notification notification, int adminUserId, CancellationToken ct)
    {
        throw new NotImplementedException();
        // var user = await _telegramUserService.GetUserByAdminIdAsync(adminUserId, ct);
        // if (user == null)
        // {
        //     _logger.LogWarning("Admin {AdminUserId} not linked to Telegram", adminUserId);
        //     return;
        // }
        //
        // var text = $"🔔 {notification.Title}\n{notification.Message}";
        // await _telegramUserService.SendMessageAsync(user.TelegramId, text, ct);
    }
}
