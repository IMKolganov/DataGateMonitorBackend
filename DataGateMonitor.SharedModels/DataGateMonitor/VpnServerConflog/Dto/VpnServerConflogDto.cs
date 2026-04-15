using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;

public class VpnServerConflogDto
{
    public int Id { get; set; }
    public int? VpnServerId { get; set; }
    public string RequestUrl { get; set; } = string.Empty;
    /// <summary>Microservice api/info response (Version, Environment, Application, Config, etc.).</summary>
    public RootInfoResponse? Payload { get; set; }
    public DateTimeOffset CreateDate { get; set; }
}
