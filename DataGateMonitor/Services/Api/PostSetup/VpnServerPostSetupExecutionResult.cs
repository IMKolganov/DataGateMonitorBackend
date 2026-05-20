using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Api.PostSetup;

public class VpnServerPostSetupExecutionResult
{
    public int VpnServerId { get; set; }
    public VpnServerType ServerType { get; set; }
    public bool CreatedDefaultConfig { get; set; }
}
