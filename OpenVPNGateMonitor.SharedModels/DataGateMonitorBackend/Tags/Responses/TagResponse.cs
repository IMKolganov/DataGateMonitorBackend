using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Responses;

public class TagResponse
{
    public TagDto Tag { get; set; } = new();
}
