using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;


namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiService(
    IUnitOfWork unitOfWork,
    IOvpnFileApiClient ovpnFileApiClient,
    ILogger<OvpnFileApiClient> logger) : IOvpnFileApiService
{
    public async Task<List<IssuedOvpnFile>> GetAllOvpnFilesAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        return  await unitOfWork.GetQuery<IssuedOvpnFile>().AsQueryable()
            .Where(x =>
                x.VpnServerId == vpnServerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IssuedOvpnFile> AddOvpnFileAsync(AddOvpnFileRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to add new OVPN file: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        var repository = unitOfWork.GetRepository<IssuedOvpnFile>();

        if (await repository.Query
                .Where(x => x.VpnServerId == request.VpnServerId && x.CommonName == request.CommonName)
                .AnyAsync(cancellationToken))
        {
            logger.LogWarning("OVPN file already exists: CommonName={CommonName}, VpnServerId={VpnServerId}",
                request.CommonName, request.VpnServerId);
            throw new Exception($"OVPN file with CommonName: '{request.CommonName}' already exists");
        }

        var result = await ovpnFileApiClient.AddOvpnFileAsync(
            request.VpnServerId,
            request.Adapt<OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests.AddOvpnFileRequest>(),
            cancellationToken);

        var issuedOvpnFile = new IssuedOvpnFile
        {
            VpnServerId = request.VpnServerId,
            CommonName = request.CommonName,
            ExternalId = request.ExternalId,
            CertId = "unavailable",
            PemFilePath = "unavailable",
            ReqFilePath = "unavailable",
            FileName = result.FileName,
            FilePath = result.FilePath,
            IssuedAt = result.IssuedAt,
            IssuedTo = result.IssuedTo,
            CertFilePath = result.CertFilePath,
            KeyFilePath = result.KeyFilePath,
            IsRevoked = false
        };

        await repository.AddAsync(issuedOvpnFile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("OVPN file added successfully: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        return await repository.Query
            .Where(x => x.VpnServerId == request.VpnServerId && x.CommonName == request.CommonName)
            .FirstAsync(cancellationToken);
    }

    public async Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeOvpnFileRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to revoke OVPN file: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        var repository = unitOfWork.GetRepository<IssuedOvpnFile>();

        var result = await ovpnFileApiClient.RevokeOvpnFileAsync(
            request.VpnServerId,
            request.Adapt<OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests.RevokeOvpnFileRequest>(),
            cancellationToken);

        var issuedOvpnFile = await repository.Query
            .Where(x => x.VpnServerId == request.VpnServerId && x.CommonName == request.CommonName)
            .FirstOrDefaultAsync(cancellationToken);

        if (issuedOvpnFile == null)
        {
            logger.LogWarning("Issued OVPN file not found for revocation: CommonName={CommonName}, VpnServerId={VpnServerId}",
                request.CommonName, request.VpnServerId);
            throw new InvalidOperationException("Issued OVPN file not found.");
        }

        issuedOvpnFile.IsRevoked = result;
        repository.Update(issuedOvpnFile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("OVPN file revoked: CommonName={CommonName}, VpnServerId={VpnServerId}",
            request.CommonName, request.VpnServerId);

        return await repository.Query
            .Where(x => x.VpnServerId == request.VpnServerId && x.CommonName == request.CommonName)
            .FirstAsync(cancellationToken);
    }

    public async Task<DownloadOvpnFileResponse> DownloadOvpnFileAsync(
        DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start downloading OVPN file: VpnServerId={VpnServerId}, IssuedOvpnFileId={IssuedOvpnFileId}",
            request.VpnServerId, request.IssuedOvpnFileId);

        var issuedOvpnFile = await unitOfWork.GetQuery<IssuedOvpnFile>().AsQueryable()
            .Where(x =>
                x.VpnServerId == request.VpnServerId && x.Id == request.IssuedOvpnFileId)
            .FirstOrDefaultAsync(cancellationToken);

        if (issuedOvpnFile == null)
        {
            logger.LogWarning("Issued OVPN file not found: VpnServerId={VpnServerId}, Id={IssuedOvpnFileId}",
                request.VpnServerId, request.IssuedOvpnFileId);
            throw new InvalidOperationException("Issued OVPN file not found.");
        }

        var requestApi =
            new OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests.DownloadOvpnFileRequest()
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
            CreatedAtUtc = DateTime.UtcNow,
            FileSizeBytes = result.Content.LongLength,
            Content = result.Content
        };
    }
}