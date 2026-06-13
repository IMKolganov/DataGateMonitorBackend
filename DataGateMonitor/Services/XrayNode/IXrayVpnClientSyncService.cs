using DataGateMonitor.Models;
using DataGateMonitor.Models.XrayNode;

namespace DataGateMonitor.Services.XrayNode;

public interface IXrayVpnClientSyncService
{
    /// <summary>
    /// Upserts <see cref="VpnServerClient"/> rows and traffic samples from the Xray agent client list.
    /// </summary>
    Task SyncConnectedClientsAsync(VpnServer server, IReadOnlyList<XrayNodeClientDto> clients,
        CancellationToken cancellationToken);
}
