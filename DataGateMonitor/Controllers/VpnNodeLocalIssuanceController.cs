using System.Text.Json;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.LocalIssuance;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.Controllers;

/// <summary>
/// Node-local ("reverse") OpenVPN issuance: nodes report certificates they issued themselves; admins manage the
/// node key that authenticates those reports.
/// </summary>
[ApiController]
[Route("api/open-vpn-servers")]
[Authorize]
public class VpnNodeLocalIssuanceController(
    ILocalIssuanceEventService localIssuanceEventService,
    INodeKeyBindingService nodeKeyBindingService,
    ILogger<VpnNodeLocalIssuanceController> logger) : BaseController
{
    private const int MaxEventBodyBytes = 64 * 1024;
    private const string NotAuthenticated = "Node signature missing, invalid, or key not trusted.";

    private static readonly JsonSerializerOptions EventJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Signed by the node key (X-DataGate-Node-Key / -Timestamp / -Signature) instead of a user JWT.
    /// Idempotent per certificate serial, so the node may retry freely.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("local-issuance-events")]
    [RequestSizeLimit(MaxEventBodyBytes)]
    public async Task<ActionResult<ApiResponse<LocalIssuanceEventResponse>>> PostLocalIssuanceEvent(CancellationToken ct)
    {
        byte[] body;
        using (var buffer = new MemoryStream())
        {
            await Request.Body.CopyToAsync(buffer, ct);
            body = buffer.ToArray();
        }

        var server = await localIssuanceEventService.AuthenticateAsync(
            Request.Headers[NodeRequestSignature.KeyIdHeader].ToString(),
            Request.Headers[NodeRequestSignature.TimestampHeader].ToString(),
            Request.Headers[NodeRequestSignature.SignatureHeader].ToString(),
            HttpMethods.Post,
            LocalIssuanceEventService.SignedPath,
            body,
            ct);
        if (server is null)
            return Unauthorized(ApiResponse<LocalIssuanceEventResponse>.ErrorResponse(NotAuthenticated));

        LocalIssuanceEventRequest? evt;
        try
        {
            evt = JsonSerializer.Deserialize<LocalIssuanceEventRequest>(body, EventJson);
        }
        catch (JsonException ex)
        {
            return BadRequest(ApiResponse<LocalIssuanceEventResponse>.ErrorResponse($"Malformed event: {ex.Message}"));
        }

        if (evt is null)
            return BadRequest(ApiResponse<LocalIssuanceEventResponse>.ErrorResponse("Empty event."));

        try
        {
            var (outcome, id) = await localIssuanceEventService.ApplyAsync(server, evt, ct);
            return Ok(ApiResponse<LocalIssuanceEventResponse>.SuccessResponse(new LocalIssuanceEventResponse
            {
                Outcome = outcome.ToString(),
                IssuedOvpnFileId = id
            }));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Rejected local issuance event from VpnServer {VpnServerId}: {Message}", server.Id, ex.Message);
            return BadRequest(ApiResponse<LocalIssuanceEventResponse>.ErrorResponse(ex.Message));
        }
    }

    [Authorize(Roles = "Admin,App")]
    [HttpGet("{vpnServerId:int}/node-key")]
    public async Task<ActionResult<ApiResponse<NodeKeyStatusResponse>>> GetNodeKey(
        [FromRoute] int vpnServerId, CancellationToken ct)
    {
        var status = await nodeKeyBindingService.GetServerStatusAsync(vpnServerId, ct);
        return status is null
            ? NotFound(ApiResponse<NodeKeyStatusResponse>.ErrorResponse("VPN server not found."))
            : Ok(ApiResponse<NodeKeyStatusResponse>.SuccessResponse(status));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPost("{vpnServerId:int}/node-key/trust")]
    public async Task<ActionResult<ApiResponse<NodeKeyStatusResponse>>> TrustNodeKey(
        [FromRoute] int vpnServerId, [FromBody] TrustNodeKeyRequest request, CancellationToken ct)
    {
        if (!await nodeKeyBindingService.TrustPendingAsync(vpnServerId, request.Fingerprint, ct))
            return BadRequest(ApiResponse<NodeKeyStatusResponse>.ErrorResponse(
                "No pending node key with this fingerprint for the server."));

        return Ok(ApiResponse<NodeKeyStatusResponse>.SuccessResponse(
            (await nodeKeyBindingService.GetServerStatusAsync(vpnServerId, ct))!));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpGet("discoveries/{discoveryId:int}/node-key")]
    public async Task<ActionResult<ApiResponse<NodeKeyStatusResponse>>> GetDiscoveryNodeKey(
        [FromRoute] int discoveryId, CancellationToken ct) =>
        Ok(ApiResponse<NodeKeyStatusResponse>.SuccessResponse(new NodeKeyStatusResponse
        {
            PendingFingerprint = await nodeKeyBindingService.GetDiscoveryFingerprintAsync(discoveryId, ct)
        }));
}
