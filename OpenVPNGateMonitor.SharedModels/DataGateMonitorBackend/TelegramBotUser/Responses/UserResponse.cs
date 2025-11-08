using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

public class UserResponse
{
    public TelegramBotUserDto TelegramBotUser { get; set; } = new();
}