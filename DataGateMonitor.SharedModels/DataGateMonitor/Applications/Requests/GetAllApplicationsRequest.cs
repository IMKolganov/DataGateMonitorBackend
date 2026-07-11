namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Requests;

public class GetAllApplicationsRequest
{
    /// <summary>Case-insensitive contains on application name.</summary>
    public string? Name { get; set; }

    /// <summary>Case-insensitive contains on OAuth client id.</summary>
    public string? ClientId { get; set; }

    public bool? IsRevoked { get; set; }
}
