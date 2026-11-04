namespace DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Responses;

public sealed class LocalIssuanceEventResponse
{
    public string Outcome { get; set; } = string.Empty;
    public int IssuedOvpnFileId { get; set; }
}
