using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.TelegramBotUser.Mappings;

public class TelegramBotUserMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterUserRequest, Models.TelegramBotUser>()
            .Ignore(dest => dest.Id);

        config.NewConfig<Models.TelegramBotUser, RegisterUserResponse>();

        config.NewConfig<Models.TelegramBotUser, TelegramBotUserDto>();

        config.NewConfig<List<Models.TelegramBotUser>, GetAdminsResponse>()
            .Map(dest => dest.TelegramBotAdmins, src => src.Adapt<List<TelegramBotUserDto>>());
    }
}