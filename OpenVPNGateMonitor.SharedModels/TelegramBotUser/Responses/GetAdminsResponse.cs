using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotUser.Responses;

public class GetAdminsResponse
{
    public List<TelegramBotUserDto> TelegramBotAdmins { get; set; } = new();
}