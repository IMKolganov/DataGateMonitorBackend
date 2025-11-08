using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

namespace OpenVPNGateMonitor.Mapping.DataGateOpenVpnManager.Mappings;

public class DataGateOpenVpnManagerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // 1) AddFileRequest -> GenerateOvpnFileRequest
        config.NewConfig<AddFileRequest, GenerateOvpnFileRequest>()
            .Map(dest => dest.CommonName,    src => src.CommonName)
            .Map(dest => dest.IssuedTo,      src => src.IssuedTo)
            // These are filled later in service
            .Ignore(dest => dest.FriendlyΝame)
            .Ignore(dest => dest.ConfigTemplate)
            .Ignore(dest => dest.ServerIp)
            .Ignore(dest => dest.ServerPort)
            .Map(dest => dest.OvpnFileExpireDays, src => src.OvpnFileExpireDays)
            ;

        // 2) (AddFileRequest request, OvpnFileMetadata meta) -> IssuedOvpnFile
        config.NewConfig<(AddFileRequest request, OvpnFileMetadata meta), IssuedOvpnFile>()
            .Map(d => d.VpnServerId, s => s.request.VpnServerId)
            .Map(d => d.ExternalId, s => s.request.ExternalId)
            .Map(d => d.CommonName, s => s.meta.CommonName) // required in meta
            .Map(d => d.FileName, s => s.meta.FileName)
            .Map(d => d.FilePath, s => s.meta.FilePath)
            .Map(d => d.IssuedAt, s => s.meta.IssuedAt)
            .Map(d => d.IssuedTo, s => s.meta.IssuedTo)
            .Map(d => d.CertFilePath, s => s.meta.CertFilePath)
            .Map(d => d.KeyFilePath, s => s.meta.KeyFilePath)
            // Fields not provided by meta — set defaults here
            .Map(d => d.CertId, _ => "unavailable")
            .Map(d => d.PemFilePath, _ => "unavailable")
            .Map(d => d.ReqFilePath, _ => "unavailable")
            .Map(d => d.IsRevoked, _ => false)
            .Map(d => d.Message, _ => string.Empty)
            // Common BaseEntity fields usually set by EF/DB triggers
            .Ignore(d => d.Id);
    }
}