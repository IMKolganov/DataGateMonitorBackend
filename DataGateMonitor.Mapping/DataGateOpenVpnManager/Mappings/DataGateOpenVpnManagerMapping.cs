using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;
using Newtonsoft.Json.Linq;

namespace DataGateMonitor.Mapping.DataGateOpenVpnManager.Mappings;

public class DataGateOpenVpnManagerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<VpnServerConflog, VpnServerConflogDto>()
            .Map(d => d.Payload, s => DeserializePayload(s.PayloadJson));

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

    private static RootOpenVpnInfoResponse? DeserializePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;
        try
        {
            var root = JObject.Parse(payloadJson);

            // Backward compatibility:
            // - old conflog rows: payload is plain RootOpenVpnInfoResponse
            // - new rows (after Xray support): payload is VpnMicroserviceDiagnosticsDto
            //   with nested { openVpn: { ... } }.
            if (root["openVpn"] is JObject openVpnObj)
                return openVpnObj.ToObject<RootOpenVpnInfoResponse>(Newtonsoft.Json.JsonSerializer.Create(ProjectJson.WebSettings));

            return ProjectJson.Deserialize<RootOpenVpnInfoResponse>(payloadJson);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return null;
        }
    }
}
