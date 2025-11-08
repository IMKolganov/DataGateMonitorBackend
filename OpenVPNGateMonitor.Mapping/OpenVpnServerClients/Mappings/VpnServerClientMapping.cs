using Mapster;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerClients.Mappings;

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