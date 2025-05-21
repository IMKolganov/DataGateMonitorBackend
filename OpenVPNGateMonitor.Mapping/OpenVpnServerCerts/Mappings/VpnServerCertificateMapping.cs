using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerCerts.Mappings;

public class VpnServerCertificateMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<(ServerCertificate cert, int vpnServerId), ServerCertificate>()
            .Map(dest => dest.CommonName, src => src.cert.CommonName)
            .Map(dest => dest.Status, src => src.cert.Status)
            .Map(dest => dest.SerialNumber, src => src.cert.SerialNumber)
            .Map(dest => dest.UnknownField, src => src.cert.UnknownField)
            .Map(dest => dest.IsRevoked, src => src.cert.IsRevoked)
            .Map(dest => dest.Message, src => src.cert.Message)
            .Map(dest => dest.CertificatePath, src => src.cert.CertificatePath)
            .Map(dest => dest.KeyPath, src => src.cert.KeyPath)
            .Map(dest => dest.ExpiryDate, src => src.cert.ExpiryDate)
            .Map(dest => dest.RevokeDate, src => src.cert.RevokeDate);
    }
}