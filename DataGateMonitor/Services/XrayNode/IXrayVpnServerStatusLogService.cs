using DataGateMonitor.Models;
using DataGateMonitor.Models.XrayNode;

namespace DataGateMonitor.Services.XrayNode;

/// <summary>
/// Writes <see cref="VpnServerStatusLog"/> for Xray polling (separate session id from OpenVPN).
/// Failures are logged and do not throw.
/// </summary>
public interface IXrayVpnServerStatusLogService
{
    Task TryAppendOrUpdateAsync(VpnServer server, XrayNodeClientsResponse payload, CancellationToken cancellationToken);
}
