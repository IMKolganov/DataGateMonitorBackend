namespace DataGateMonitor.Services.DataGateXRayManager.ClientLinks;

/// <summary>JSON contract for DataGateXRayManager <c>api/client-links/add</c> request body.</summary>
public sealed class GenerateClientLinkMicroserviceRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string ConfigTemplate { get; set; } = string.Empty;
    public string ServerIp { get; set; } = string.Empty;
    public int ServerPort { get; set; }
    public string IssuedTo { get; set; } = "xrayClient";
    public int LinkExpireDays { get; set; } = 365;
}

public sealed class RevokeClientLinkMicroserviceRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public sealed class DownloadClientLinkMicroserviceRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public sealed class ClientLinkMetadataDto
{
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string IssuedTo { get; set; } = string.Empty;
    public string? CertFilePath { get; set; }
    public string? KeyFilePath { get; set; }
}

public sealed class ClientLinkDownloadDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
