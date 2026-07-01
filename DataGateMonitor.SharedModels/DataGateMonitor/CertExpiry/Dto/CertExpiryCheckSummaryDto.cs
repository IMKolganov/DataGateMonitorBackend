namespace DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;

public class CertExpiryCheckSummaryDto
{
    public int ServersChecked { get; set; }
    public int ProfilesChecked { get; set; }
    public int Healthy { get; set; }
    public int ExpiringSoon { get; set; }
    public int Expired { get; set; }
    public int MissingOnNode { get; set; }
    public int ServerFailures { get; set; }
}
