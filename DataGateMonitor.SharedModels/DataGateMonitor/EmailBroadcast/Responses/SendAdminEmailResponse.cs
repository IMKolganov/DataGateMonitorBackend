namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;

public class SendAdminEmailResponse
{
    public int Attempted { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
}
