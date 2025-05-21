using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;

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

        
        // config.NewConfig<ServerCertificate, VpnServerCertificateResponse>()
        //     .Map(dest => dest.CommonName, src => src.CommonName)
        //     .Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
        //     .Map(dest => dest.RevokeDate, src => src.RevokeDate)
        //     .Map(dest => dest.SerialNumber, src => src.SerialNumber)
        //     .Map(dest => dest.UnknownField, src => src.UnknownField)
        //     .Map(dest => dest.IsRevoked, src => src.Status == CertificateStatus.Revoked);
        //
        //
        // config.NewConfig<OpenVpnServerCertConfig, ServerCertConfigResponse>();
        //
        // config.NewConfig<CertificateRevokeResult, RevokeCertificateResponse>()
        //     .Map(dest => dest.IsRevoked, src => src.IsRevoked)
        //     .Map(dest => dest.Message, src => src.Message)
        //     .Map(dest => dest.CertificatePath, src => src.CertificatePath);
        //
        // config.NewConfig<OpenVpnServerCertConfig, UpdateServerCertConfigResponse>()
        //     .Map(dest => dest.VpnServerId, src => src.VpnServerId)
        //     .Map(dest => dest.Success, _ => true)
        //     .Map(dest => dest.Message, _ => "Server certificate configuration updated successfully.");

    }
}