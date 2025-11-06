using System.Text.RegularExpressions;
using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager;

public class OvpnFileApiService(IOvpnFileApiClient ovpnFileApiClient, 
    ILogger<OvpnFileApiClient> logger, IConfiguration configuration,
    IOpenVpnServerOvpnFileConfigQueryService openVpnServerOvpnFileConfigQueryService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IIssuedOvpnFileTokenQueryService issuedOvpnFileTokenQueryService,
    ICommandService<IssuedOvpnFile, int> issuedOvpnFileCommandService,
    ICommandService<IssuedOvpnFileToken, int> issuedOvpnFileTokenCommandService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOvpnFileNotificationService ovpnFileNotificationService) : IOvpnFileApiService
{
    private const int DefaultTokenExpireDays = 1;

    private int TokenExpireDays =>
        configuration.GetValue<int?>("OvpnFileToken:ExpireDays") ?? DefaultTokenExpireDays;

    public async Task<IssuedOvpnFile> GetByTokenAsync(string token, CancellationToken cancellationToken,
        bool isRevoked = false)
    {
        var issuedOvpnFileToken = await issuedOvpnFileTokenQueryService.GetByTokenAsync(token, cancellationToken)
            ?? throw new InvalidOperationException("IssuedOvpnFileToken not found");

        var issuedOvpnFile = await issuedOvpnFileQueryService.GetByIdAndIsRevokedAsync(issuedOvpnFileToken.IssuedOvpnFileId, 
                   isRevoked, cancellationToken)
               ?? throw new InvalidOperationException("IssuedOvpnFile not found");
                
        await ovpnFileNotificationService.NotifyReadByTokenAsync(token, issuedOvpnFile.Id, 
            issuedOvpnFile.VpnServerId, isRevoked, cancellationToken);
        
        return issuedOvpnFile;
    }
    
    public async Task<List<IssuedOvpnFile>> GetAllByExternalIdAsync(string externalId, 
        CancellationToken cancellationToken)
    {
        var issuedFiles = await issuedOvpnFileQueryService.GetAllByExternalId(
            externalId, cancellationToken);
        
        await ovpnFileNotificationService.NotifyReadByExternalIdAsync(externalId, issuedFiles.Count, cancellationToken);

        return issuedFiles;
    }
    
    public async Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var issuedFiles = await issuedOvpnFileQueryService.GetAllByVpnServerId(vpnServerId, 
            cancellationToken);
        
        await ovpnFileNotificationService.NotifyReadAllAsync(vpnServerId, issuedFiles.Count, cancellationToken);

        return issuedFiles;
    }

    public async Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByVpnServerIdWithTokenAsync(
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

        
        await ovpnFileNotificationService.NotifyReadAllWithTokenAsync(vpnServerId, result.Count, isRevoked,
            cancellationToken);
        
        return result;
    }

    public async Task<List<IssuedOvpnFile>> GetAllByExternalIdAndVpnServerIdAsync(int vpnServerId, string externalId,
        CancellationToken cancellationToken, bool isRevoked = false)
    {
        var result = await issuedOvpnFileQueryService.GetAllByVpnServerIdAndExternalIdAndIsRevoked(
            vpnServerId, externalId, isRevoked, cancellationToken);
        await ovpnFileNotificationService.NotifyReadByExternalIdAndVpnServerIdAsync(vpnServerId, externalId, result.Count,
            isRevoked, cancellationToken);

        return result;
    }

    public async Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByExternalIdAndVpnServerIdWithTokenAsync(
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
        
        await ovpnFileNotificationService.NotifyReadByExternalIdWithTokenAsync(vpnServerId, externalId, result.Count, 
            isRevoked, cancellationToken);

        return result;
    }

    public async Task<(IssuedOvpnFile File, IssuedOvpnFileToken Token)> AddOvpnFileWithTokenAsync(
        AddFileRequest request,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFile = await AddOvpnFileAsync(request, cancellationToken);
        var token = await MakeTokenForFileAsync(issuedOvpnFile, cancellationToken);

        
        await ovpnFileNotificationService.NotifyIssuedWithTokenAsync(
            issuedOvpnFile.VpnServerId, issuedOvpnFile.Id, issuedOvpnFile.FileName,
            issuedOvpnFile.ExternalId, token.Id, /* todo: user ID*/ cancellationToken);
        
        return (issuedOvpnFile, token);
    }
    
    public async Task<IssuedOvpnFile> AddOvpnFileAsync(AddFileRequest request, 
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
        
        
        await ovpnFileNotificationService.NotifyIssuedAsync(
            issuedOvpnFile.VpnServerId, issuedOvpnFile.Id, issuedOvpnFile.FileName,
            issuedOvpnFile.ExternalId, /* todo: user ID*/ ct);

        
        return await issuedOvpnFileQueryService.GetByVpnServerIdAndCommonNameAsync(
            request.VpnServerId, request.CommonName, ct)
               ?? throw new InvalidOperationException("Issued OVPN file not found.");
    }

    public async Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeFileRequest request, CancellationToken ct)
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
        
        await ovpnFileNotificationService.NotifyRevokedAsync(
            issuedOvpnFile.VpnServerId, issuedOvpnFile.Id, issuedOvpnFile.FileName,
            issuedOvpnFile.ExternalId, /* todo: user ID*/ ct);

        return await issuedOvpnFileQueryService.GetByVpnServerIdAndCommonNameAsync(request.VpnServerId,
            request.CommonName, ct) ?? throw new InvalidOperationException("Issued OVPN file not found.");
    }

    public async Task<DownloadFileResponse> DownloadOvpnFileAsync(
        DownloadFileRequest request,
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
        
        await ovpnFileNotificationService.NotifyDownloadedAsync(
            issuedOvpnFile.VpnServerId, issuedOvpnFile.FileName, issuedOvpnFile.ExternalId,
            isRevoked, /* todo: user ID*/ cancellationToken);

        return new DownloadFileResponse
        {
            IssuedOvpn = issuedOvpnFile.Adapt<IssuedOvpnFileDto>(),
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