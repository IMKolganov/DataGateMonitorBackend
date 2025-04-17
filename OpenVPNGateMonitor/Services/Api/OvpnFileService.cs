using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.EasyRsaServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Services.Api;

public class OvpnFileService : IOvpnFileService
{
    private readonly ILogger<IOvpnFileService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertVpnService _certVpnService;
    private readonly IEasyRsaService _easyRsaService;

    public OvpnFileService(ILogger<IOvpnFileService> logger, IUnitOfWork unitOfWork, 
        ICertVpnService certVpnService, IEasyRsaService easyRsaService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _certVpnService = certVpnService;
        _easyRsaService = easyRsaService;
    }

    public async Task<List<IssuedOvpnFile>> GetAllOvpnFiles(int vpnServerId, 
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<IssuedOvpnFile>()
            .AsQueryable().Where(x=> x.VpnServerId == vpnServerId)
            .OrderBy(x=>x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<IssuedOvpnFile>> GetAllOvpnFilesByExternalId(int vpnServerId, string externalId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<IssuedOvpnFile>()
            .AsQueryable().Where(x=> x.VpnServerId == vpnServerId && x.ExternalId == externalId)
            .OrderBy(x=>x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<AddOvpnFileResponse> AddOvpnFile(
        string externalId,
        string commonName,
        int vpnServerId,
        CancellationToken cancellationToken,
        string issuedTo = "openVpnClient")
    {
        if (await _unitOfWork
                .GetQuery<IssuedOvpnFile>()
                .AsQueryable()
                .AnyAsync(x => x.CommonName == commonName && !x.IsRevoked, cancellationToken))
        {
            throw new InvalidOperationException($"OVPN file with CommonName '{commonName}' already exists.");
        }

        var certConfig = await _unitOfWork.GetQuery<OpenVpnServerCertConfig>()
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.VpnServerId == vpnServerId, cancellationToken)
                         ?? throw new InvalidOperationException(
                             "OpenVpnServerCertConfig not found for given VpnServerId.");

        var fileConfig = await _unitOfWork.GetQuery<OpenVpnServerOvpnFileConfig>()
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.VpnServerId == vpnServerId, cancellationToken)
                         ?? throw new InvalidOperationException(
                             $"OVPN server config missing for server ID {vpnServerId}. " +
                             $"Please configure OpenVpnServerOvpnFileConfig first.");

        _logger.LogInformation("Step 1: Building client certificate...");
        var certResult = await _certVpnService.AddServerCertificate(vpnServerId, commonName, cancellationToken);

        _logger.LogInformation("Step 2: Reading certificate and key content...");
        var caCertContent =
            _easyRsaService.ReadPemContent(certConfig.CaCertPath ??
                                           throw new InvalidOperationException("CaCertPath is null."));
        var clientCertContent = _easyRsaService.ReadPemContent(certResult.CertificatePath ??
                                                               throw new InvalidOperationException(
                                                                   "CertificatePath is null."));
        var clientKeyContent =
            await File.ReadAllTextAsync(certResult.KeyPath ?? throw new InvalidOperationException("KeyPath is null."),
                cancellationToken);

        _logger.LogInformation("Step 3: Generating .ovpn file...");
        var ovpnContent = GenerateOvpnFile(
            fileConfig.ConfigTemplate,
            fileConfig.VpnServerIp,
            fileConfig.VpnServerPort,
            caCertContent,
            clientCertContent,
            clientKeyContent,
            certConfig.TlsAuthKey);

        _logger.LogInformation("Step 4: Writing .ovpn file...");

        var targetDir = certConfig.OvpnFileDir ?? throw new InvalidOperationException("OvpnFileDir is null.");
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var ovpnFilePath = Path.Combine(targetDir, $"{commonName}.ovpn");
        await File.WriteAllTextAsync(ovpnFilePath, ovpnContent, cancellationToken);

        _logger.LogInformation("Client configuration file created: {Path}", ovpnFilePath);

        var fileInfo = new FileInfo(Path.GetFullPath(ovpnFilePath));
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("OVPN file was not created as expected.", fileInfo.FullName);
        }

        _logger.LogInformation("Step 5: Saving metadata in database...");
        var issuedOvpnFile = new IssuedOvpnFile
        {
            VpnServerId = vpnServerId,
            ExternalId = externalId,
            CommonName = commonName,
            CertId = certResult.CertId,
            FileName = fileInfo.Name,
            FilePath = fileInfo.FullName,
            IssuedAt = DateTime.UtcNow,
            IssuedTo = issuedTo,
            CertFilePath = certResult.CertificatePath,
            KeyFilePath = certResult.KeyPath,
            ReqFilePath = certResult.RequestPath,
            PemFilePath = certResult.PemPath,
            IsRevoked = false
        };

        await SaveInfoInDataBase(issuedOvpnFile, cancellationToken);
        var issuedOvpnFileDto = issuedOvpnFile.Adapt<IssuedOvpnFileDto>();

        return new AddOvpnFileResponse
        {
            IssuedOvpnFile = issuedOvpnFileDto
        };
    }

    public async Task<IssuedOvpnFile?> RevokeOvpnFile(int vpnServerId, string commonName,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFile = await _unitOfWork.GetQuery<IssuedOvpnFile>()
                                 .AsQueryable()
                                 .Where(x => x.VpnServerId == vpnServerId && x.CommonName == commonName)
                                 .FirstOrDefaultAsync(cancellationToken) 
                             ?? throw new InvalidOperationException("IssuedOvpnFile not found");
        
        var openVpnServerCertConfig = await _unitOfWork.GetQuery<OpenVpnServerCertConfig>()
            .AsQueryable()
            .Where(x => x.VpnServerId == issuedOvpnFile.VpnServerId)
            .FirstOrDefaultAsync(cancellationToken) 
                                      ?? throw new InvalidOperationException("OpenVpnServerCertConfig not found");
        
        var certificateRevokeResult = _easyRsaService.RevokeCertificate(openVpnServerCertConfig, issuedOvpnFile.CommonName);
        
        _logger.LogInformation($"RevokeCertificate result: {certificateRevokeResult.Message} " +
                               $"for CertName: {issuedOvpnFile.CommonName}");
        string revokedFilePath = MoveRevokedOvpnFile(openVpnServerCertConfig, issuedOvpnFile);
        _logger.LogInformation($"Successfully moved revoked .ovpn file to: {revokedFilePath}");

        _logger.LogInformation($"Updated database for revoked certificate: {issuedOvpnFile.CommonName}, " +
                               $"External ID: {issuedOvpnFile.ExternalId}");
        var repositoryIssuedOvpnFile = _unitOfWork.GetRepository<IssuedOvpnFile>();
        issuedOvpnFile.FilePath = revokedFilePath;
        issuedOvpnFile.IsRevoked = true;
        issuedOvpnFile.Message = certificateRevokeResult.Message;
        repositoryIssuedOvpnFile.Update(issuedOvpnFile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        issuedOvpnFile = await _unitOfWork.GetQuery<IssuedOvpnFile>()
                                 .AsQueryable()
                                 .Where(x => x.VpnServerId == vpnServerId && x.CommonName == commonName && x.IsRevoked)
                                 .FirstOrDefaultAsync(cancellationToken) 
                             ?? throw new InvalidOperationException("IssuedOvpnFile not found");
        
        return issuedOvpnFile;
    }

    public async Task<OvpnFileResult> GetOvpnFile(int issuedOvpnFileId, int vpnServerId,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFile = await _unitOfWork.GetQuery<IssuedOvpnFile>()
                                 .AsQueryable()
                                 .Where(x =>
                                     x.Id == issuedOvpnFileId && x.VpnServerId == vpnServerId)
                                 .FirstOrDefaultAsync(cancellationToken)
                             ?? throw new InvalidOperationException("OpenVpnServerCertConfig not found");
        var issuedOvpnFileStream = new FileStream(issuedOvpnFile.FilePath, FileMode.Open, FileAccess.Read);
        return new OvpnFileResult(){ FileName = issuedOvpnFile.FileName, FileStream = issuedOvpnFileStream };
    }
    
    private string MoveRevokedOvpnFile(OpenVpnServerCertConfig openVpnServerCertConfig, IssuedOvpnFile issuedOvpnFile)
    {
        string ovpnFilePath = Path.Combine(openVpnServerCertConfig.OvpnFileDir, issuedOvpnFile.FileName);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueFileName = 
            $"{Path.GetFileNameWithoutExtension(issuedOvpnFile.FileName)}" +
            $"_{issuedOvpnFile.Id}" +
            $"_{timestamp}" +
            $"{Path.GetExtension(issuedOvpnFile.FileName)}";

        string revokedFilePath = Path.Combine(openVpnServerCertConfig.RevokedOvpnFilesDirPath, uniqueFileName);

        Directory.CreateDirectory(openVpnServerCertConfig.OvpnFileDir);
        Directory.CreateDirectory(openVpnServerCertConfig.RevokedOvpnFilesDirPath);

        if (File.Exists(ovpnFilePath))
        {
            File.Move(ovpnFilePath, revokedFilePath);
            _logger.LogInformation($"Moved .ovpn file to revoked folder: {revokedFilePath}");
        }
        else
        {
            _logger.LogWarning($".ovpn file not found for moving: {ovpnFilePath}");
        }

        return revokedFilePath;
    }
    
    private async Task SaveInfoInDataBase(IssuedOvpnFile issuedOvpnFile, CancellationToken cancellationToken)
    {
        var repositoryIssuedOvpnFile = _unitOfWork.GetRepository<IssuedOvpnFile>();
        await repositoryIssuedOvpnFile.AddAsync(issuedOvpnFile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateOvpnFile(
        string configTemplate,
        string serverIp,
        int serverPort,
        string caCert,
        string clientCert,
        string clientKey,
        string tlsAuthKeyPath)
    {
        if (string.IsNullOrWhiteSpace(configTemplate))
            throw new ArgumentNullException(nameof(configTemplate));
        if (string.IsNullOrWhiteSpace(serverIp))
            throw new ArgumentNullException(nameof(serverIp));
        if (string.IsNullOrWhiteSpace(caCert))
            throw new ArgumentNullException(nameof(caCert));
        if (string.IsNullOrWhiteSpace(clientCert))
            throw new ArgumentNullException(nameof(clientCert));
        if (string.IsNullOrWhiteSpace(clientKey))
            throw new ArgumentNullException(nameof(clientKey));
        if (string.IsNullOrWhiteSpace(tlsAuthKeyPath))
            throw new ArgumentNullException(nameof(tlsAuthKeyPath));

        var tlsAuthKey = File.ReadAllText(tlsAuthKeyPath);

        return configTemplate
            .Replace("{{server_ip}}", serverIp)
            .Replace("{{server_port}}", serverPort.ToString())
            .Replace("{{ca_cert}}", caCert)
            .Replace("{{client_cert}}", clientCert)
            .Replace("{{client_key}}", clientKey)
            .Replace("{{tls_auth_key}}", tlsAuthKey);
    }
}