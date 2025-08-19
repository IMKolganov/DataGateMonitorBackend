using System.Text.RegularExpressions;
using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiService(IOvpnFileApiClient ovpnFileApiClient, 
    ILogger<OvpnFileApiClient> logger, IConfiguration configuration,
    IOpenVpnServerOvpnFileConfigQueryService openVpnServerOvpnFileConfigQueryService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IIssuedOvpnFileTokenQueryService issuedOvpnFileTokenQueryService,
    ICommandService<IssuedOvpnFile, int> issuedOvpnFileCommandService,
    ICommandService<IssuedOvpnFileToken, int> issuedOvpnFileTokenCommandService,
    IOpenVpnServerQueryService openVpnServerQueryService) : IOvpnFileApiService
{
    private const int DefaultTokenExpireDays = 1;

    private int TokenExpireDays =>
        configuration.GetValue<int?>("OvpnFileToken:ExpireDays") ?? DefaultTokenExpireDays;

    public async Task<IssuedOvpnFile> GetOvpnFileByTokenAsync(string token, CancellationToken cancellationToken,
        bool isRevoked = false)
    {
        var issuedOvpnFileToken = await issuedOvpnFileTokenQueryService.GetByTokenAsync(token, cancellationToken)
            ?? throw new InvalidOperationException("IssuedOvpnFileToken not found");

        return await issuedOvpnFileQueryService.GetByIdAndIsRevokedAsync(issuedOvpnFileToken.IssuedOvpnFileId, 
                   isRevoked, cancellationToken)
               ?? throw new InvalidOperationException("IssuedOvpnFile not found");
    }
    
    public async Task<List<IssuedOvpnFile>> GetAllOvpnFilesAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        return  await issuedOvpnFileQueryService.GetAllByVpnServerId(vpnServerId, cancellationToken);
    }

    public async Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllOvpnFilesWithTokenAsync(
        int vpnServerId, CancellationToken cancellationToken, bool isRevoked = false)
    {
        var issuedFiles = await issuedOvpnFileQueryService.GetAllByVpnServerIdAndIsRevoked(
            vpnServerId, isRevoked, cancellationToken);
        
        var tokens = await issuedOvpnFileTokenQueryService.GetByIssuedFileIdsAsync(
            issuedFiles.Select(x => x.Id).ToList(), cancellationToken);

        var result = issuedFiles.Select(file =>
        {
            var token = tokens.FirstOrDefault(t => t.IssuedOvpnFileId == file.Id);
            return (File: file, Token: token);
        }).ToList();

        return result;
    }

    public async Task<List<IssuedOvpnFile>> GetAllByExternalIdOvpnFilesAsync(int vpnServerId, string externalId,
        CancellationToken cancellationToken, bool isRevoked = false)
    {
        return await issuedOvpnFileQueryService.GetAllByVpnServerIdAndExternalIdAndIsRevoked(vpnServerId, externalId,
            isRevoked, cancellationToken);
    }

    public async Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByExternalIdOvpnFilesWithTokenAsync(
        int vpnServerId, string externalId, CancellationToken cancellationToken, bool isRevoked = false)
    {
        var issuedFiles =
            await issuedOvpnFileQueryService.GetAllByVpnServerIdAndExternalIdAndIsRevoked(vpnServerId, externalId,
                isRevoked, cancellationToken);

        var tokens = await issuedOvpnFileTokenQueryService.GetByIssuedFileIdsAsync(
            issuedFiles.Select(x => x.Id).ToList(), cancellationToken);

        var result = issuedFiles
            .Select(file =>
            {
                var token = tokens.FirstOrDefault(t => t.IssuedOvpnFileId == file.Id);
                return (File: file, Token: token);
            })
            .ToList();

        return result;
    }

    public async Task<(IssuedOvpnFile File, IssuedOvpnFileToken Token)> AddOvpnFileWithTokenAsync(
        AddClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFile = await AddOvpnFileAsync(request, cancellationToken);
        var token = await MakeTokenForFileAsync(issuedOvpnFile, cancellationToken);

        return (issuedOvpnFile, token);
    }
    
    public async Task<IssuedOvpnFile> AddOvpnFileAsync(AddClientOvpnFileRequest request, 
        CancellationToken ct)
    {
        logger.LogInformation("Attempting to add new OVPN file: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);
        
        if (await issuedOvpnFileQueryService
                .ExistsActiveByVpnServerIdAndCommonNameAsync(request.VpnServerId, request.CommonName, ct))
        {
            logger.LogWarning("OVPN file already exists: CommonName={CommonName}, VpnServerId={VpnServerId}",
                request.CommonName, request.VpnServerId);
            throw new Exception($"OVPN file with CommonName: '{request.CommonName}' already exists");
        }

        var ovpnFileConfig = await GetOpenVpnServerOvpnFileConfig(request.VpnServerId, ct);

        var generateOvpnFileRequest = request.Adapt<GenerateOvpnFileRequest>();
        generateOvpnFileRequest.FriendlyΝame = 
            await MakeFriendlyName(request.VpnServerId, request.CommonName, ct);
        generateOvpnFileRequest.ConfigTemplate = ovpnFileConfig.ConfigTemplate;
        generateOvpnFileRequest.ServerIp = ovpnFileConfig.VpnServerIp;
        generateOvpnFileRequest.ServerPort = ovpnFileConfig.VpnServerPort;
        
        var result = await ovpnFileApiClient.AddOvpnFileAsync(
            request.VpnServerId,
            generateOvpnFileRequest,
            ct);

        var issuedOvpnFile = (request, result).Adapt<IssuedOvpnFile>();
        issuedOvpnFile.CertId = "unavailable";
        issuedOvpnFile.PemFilePath = "unavailable";
        issuedOvpnFile.ReqFilePath = "unavailable";
        issuedOvpnFile.IsRevoked = false;

        await issuedOvpnFileCommandService.AddAsync(issuedOvpnFile, true, ct);

        logger.LogInformation("OVPN file added successfully: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        return await issuedOvpnFileQueryService.GetByVpnServerIdAndCommonNameAsync(
            request.VpnServerId, request.CommonName, ct)
               ?? throw new InvalidOperationException("Issued OVPN file not found.");
    }

    public async Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeClientOvpnFileRequest request, CancellationToken ct)
    {
        logger.LogInformation("Attempting to revoke OVPN file: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);
        var issuedOvpnFile = await issuedOvpnFileQueryService.GetByVpnServerIdAndCommonNameAndIsRevokedAsync(
            request.VpnServerId, request.OvpnFileId, request.CommonName, request.IsRevoked, ct);
        
        if (issuedOvpnFile == null)
        {
            logger.LogWarning("Issued OVPN file not found for revocation: CommonName={CommonName}, VpnServerId={VpnServerId}",
                request.CommonName, request.VpnServerId);
            throw new InvalidOperationException("Issued OVPN file not found.");
        }
        
        var revokeOvpnFileRequest = request.Adapt<RevokeOvpnFileRequest>();
        revokeOvpnFileRequest.OvpnFileName = issuedOvpnFile?.FileName ?? 
                                             throw new InvalidOperationException("FileName is null");
        revokeOvpnFileRequest.OvpnFilePath = issuedOvpnFile.FilePath;
            
        var result = await ovpnFileApiClient.RevokeOvpnFileAsync(
            request.VpnServerId,
            revokeOvpnFileRequest,
            ct);



        issuedOvpnFile.IsRevoked = result;
        
        await issuedOvpnFileCommandService.UpdateAsync(issuedOvpnFile, true, ct);


        logger.LogInformation("OVPN file revoked: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        return await issuedOvpnFileQueryService.GetByVpnServerIdAndCommonNameAsync(request.VpnServerId,
            request.CommonName, ct) ?? throw new InvalidOperationException("Issued OVPN file not found.");
    }

    public async Task<DownloadOvpnFileResponse> DownloadOvpnFileAsync(
        DownloadClientOvpnFileRequest request,
        CancellationToken cancellationToken, bool isRevoked = false)
    {
        logger.LogInformation("Start downloading OVPN file: VpnServerId={VpnServerId}, IssuedOvpnFileId={IssuedOvpnFileId}",
            request.VpnServerId, request.IssuedOvpnFileId);

        var issuedOvpnFile = await issuedOvpnFileQueryService.GetByIdAndVpnServerIdAndIsRevokedAsync(
                request.IssuedOvpnFileId, request.VpnServerId, isRevoked, cancellationToken)
            ?? throw new InvalidOperationException("Issued OVPN file not found.");

        var requestApi =
            new DownloadOvpnFileRequest()
            {
                CommonName = issuedOvpnFile.CommonName,
                FileName = issuedOvpnFile.FileName,
                FilePath = issuedOvpnFile.FilePath
            };
        var result = await ovpnFileApiClient.DownloadOvpnFileAsync(
            request.VpnServerId, requestApi, cancellationToken);

        if (result.Content == null)
        {
            logger.LogError("Downloaded OVPN file content is null: FileName={FileName}, CommonName={CommonName}",
                result.FileName, result.CommonName);
            throw new InvalidOperationException("Content is null.");
        }

        logger.LogInformation("Successfully downloaded OVPN file: FileName={FileName}, Size={Size} bytes",
            result.FileName, result.Content.LongLength);

        return new DownloadOvpnFileResponse
        {
            IssuedOvpnFileId = request.IssuedOvpnFileId,
            FileName = result.FileName,
            FullPath = issuedOvpnFile.FilePath,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            FileSizeBytes = result.Content.LongLength,
            Content = result.Content
        };
    }

    private async Task<IssuedOvpnFileToken> MakeTokenForFileAsync(IssuedOvpnFile issuedOvpnFile, CancellationToken ct)
    {

        var token = new IssuedOvpnFileToken
        {
            IssuedOvpnFileId = issuedOvpnFile.Id,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(TokenExpireDays),
            IsUsed = false,
            Purpose = "download"
        };

        await issuedOvpnFileTokenCommandService.AddAsync(token, true, ct);
        return token;
    }

    private async Task<OpenVpnServerOvpnFileConfig> GetOpenVpnServerOvpnFileConfig(int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnServerOvpnFileConfigQueryService.GetByVpnServerIdIdAsync(vpnServerId, ct) 
               ?? throw new InvalidOperationException("OpenVPN File Server Config not found");
    }
    
    private async Task<string> MakeFriendlyName(int vpnServerId, string commonName, CancellationToken ct)
    {
        var vpnServer = await openVpnServerQueryService.GetByIdAsync(vpnServerId, ct)
                        ?? throw new InvalidOperationException("OpenVPN Server not found");

        var lastNumber = ExtractLastNumber(commonName);

        return lastNumber is not null
            ? $"{vpnServer.ServerName} [{lastNumber}]"
            : vpnServer.ServerName;
    }
    
    private string? ExtractLastNumber(string input)
    {
        var match = Regex.Match(input, @"(\d+)$");
        return match.Success ? match.Value : null;
    }
}