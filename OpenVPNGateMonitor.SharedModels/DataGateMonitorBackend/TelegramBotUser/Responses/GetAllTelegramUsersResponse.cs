using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

public class GetAllTelegramUsersResponse
{
    public List<TelegramBotUserDto> TelegramBotUsers { get; set; } = new();
}