using DataGateMonitor.Configurations;
using DataGateMonitor.SharedModels.ClientIdentity;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateMonitor.Middlewares;

/// <summary>
/// Rejects every request that does not carry <see cref="ClientIdentityRules.NameHeader"/> and
/// <see cref="ClientIdentityRules.VersionHeader"/> (rules come from SharedModels).
/// Only CORS preflight, health probes, the root status page, swagger and the error pages are exempt.
/// Rejection is gated by <see cref="ClientIdentityOptions.Enforce"/> (read per request, so appsettings reload applies live).
/// </summary>
public sealed class ClientIdentityMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ClientIdentityOptions> options,
    ILogger<ClientIdentityMiddleware> logger)
{
    public const string HttpContextItemKey = "ClientIdentity";

    private static readonly PathString HubsPrefix = new("/api/hubs");

    private static readonly PathString[] ExemptPrefixes =
    [
        new("/health"),
        new("/swagger"),
        new("/swaggerjson"),
        new("/error")
    ];

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request))
        {
            await next(context);
            return;
        }

        var (name, version) = ReadIdentity(context.Request);
        var result = ClientIdentityRules.Validate(name, version);
        if (!result.IsValid && !options.CurrentValue.Enforce)
        {
            await next(context);
            return;
        }

        if (!result.IsValid)
        {
            logger.LogDebug(
                "Rejected request without valid client identity. Code: {Code}. Method: {Method}. Path: {Path}.",
                result.ErrorCode,
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var payload = new ApiResponse<ClientIdentityError>
            {
                Success = false,
                Message = result.ErrorMessage!,
                Data = new ClientIdentityError { Code = result.ErrorCode! }
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(payload, JsonSettings));
            return;
        }

        context.Items[HttpContextItemKey] = result.Identity;
        await next(context);
    }

    private static bool IsExempt(HttpRequest request)
    {
        if (HttpMethods.IsOptions(request.Method))
            return true;

        var path = request.Path;
        if (!path.HasValue || path.Value == "/")
            return true;

        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static (string? Name, string? Version) ReadIdentity(HttpRequest request)
    {
        string? name = request.Headers[ClientIdentityRules.NameHeader];
        string? version = request.Headers[ClientIdentityRules.VersionHeader];

        // Browser WebSockets cannot set headers; SignalR hubs may pass the identity in the query string.
        if (request.Path.StartsWithSegments(HubsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
                name = request.Query[ClientIdentityRules.NameQueryParameter];
            if (string.IsNullOrWhiteSpace(version))
                version = request.Query[ClientIdentityRules.VersionQueryParameter];
        }

        return (name, version);
    }
}
