using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;

public class AnnounceVpnServerRequest
{
    public VpnServerType ServerType { get; set; }

    [Required(ErrorMessage = "ApiUrl is required.")]
    public string ApiUrl { get; set; } = string.Empty;

    public string? SuggestedName { get; set; }

    public string? PublicIp { get; set; }

    public string? Version { get; set; }

    public bool IsEnableWss { get; set; }

    /// <summary>Null when an older node does not report Pi-hole capabilities.</summary>
    public bool? HasPiHole { get; set; }

    /// <summary>Bootstrap credentials, accepted for pending discoveries only.</summary>
    public AnnouncePiHoleConfig? PiHole { get; set; }

    /// <summary>Optional node event-signing public key; older nodes omit it.</summary>
    [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? NodePublicKeyPem { get; set; }
}
