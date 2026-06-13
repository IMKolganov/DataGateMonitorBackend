using Mapster;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

namespace DataGateMonitor.Mapping.VpnServerClients.Mappings;

public class VpnServerClientMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region "get-all-connected"
        config.NewConfig<VpnClientInfoResponseList, ConnectedClientsResponse>()
            .Map(d => d.TotalCount, s => s.TotalCount)
            .Map(d => d.VpnClients,    s => s.VpnClientInfoResponse);
        #endregion
    }
}