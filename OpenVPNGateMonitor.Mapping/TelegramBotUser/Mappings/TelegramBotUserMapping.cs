using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.TelegramBotUser.Mappings;

public class TelegramBotUserMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // entity -> dto
        config.NewConfig<Models.TelegramBotUser, TelegramBotUserDto>();

        #region single user -> UserResponse
        config.NewConfig<Models.TelegramBotUser, UserResponse>()
            .Map(dest => dest.TelegramBotUser, src => src);
        #endregion

        #region get all users
        config.NewConfig<List<Models.TelegramBotUser>, GetAllTelegramUsersResponse>()
            .Map(dest => dest.TelegramBotUsers, src =>
                src.Adapt<List<TelegramBotUserDto>>());
        #endregion

        #region get admins
        config.NewConfig<List<Models.TelegramBotUser>, GetAdminsResponse>()
            .Map(dest => dest.TelegramBotAdmins, src =>
                src.Adapt<List<TelegramBotUserDto>>());
        #endregion
    }
}