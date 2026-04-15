using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

public class UserResponse
{
    public TelegramBotUserDto TelegramBotUser { get; set; } = new();
}