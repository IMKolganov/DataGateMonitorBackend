using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Responses;

public class TagsResponse
{
    public List<TagDto> Tags { get; set; } = [];
}
