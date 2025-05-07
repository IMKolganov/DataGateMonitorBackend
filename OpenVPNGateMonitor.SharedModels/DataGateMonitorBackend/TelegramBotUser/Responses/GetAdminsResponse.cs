using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

public class GetAdminsResponse
{
    public List<TelegramBotUserDto> TelegramBotAdmins { get; set; } = new();
}