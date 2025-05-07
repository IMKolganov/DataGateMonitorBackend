using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerCerts.Mappings;

public class VpnServerCertificateMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ServerCertificate, VpnServerCertificateResponse>()
            .Map(dest => dest.CommonName, src => src.CommonName)
            .Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
            .Map(dest => dest.RevokeDate, src => src.RevokeDate)
            .Map(dest => dest.SerialNumber, src => src.SerialNumber)
            .Map(dest => dest.UnknownField, src => src.UnknownField)
            .Map(dest => dest.IsRevoked, src => src.Status == CertificateStatus.Revoked);


        config.NewConfig<OpenVpnServerCertConfig, ServerCertConfigResponse>();

        config.NewConfig<CertificateRevokeResult, RevokeCertificateResponse>()
            .Map(dest => dest.IsRevoked, src => src.IsRevoked)
            .Map(dest => dest.Message, src => src.Message)
            .Map(dest => dest.CertificatePath, src => src.CertificatePath);

        config.NewConfig<OpenVpnServerCertConfig, UpdateServerCertConfigResponse>()
            .Map(dest => dest.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.Success, _ => true)
            .Map(dest => dest.Message, _ => "Server certificate configuration updated successfully.");

        config.NewConfig<UpdateServerCertConfigRequest, OpenVpnServerCertConfigInfo>();
    }
}