using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class HttpErrorMapper(ILogger<HttpErrorMapper> logger) : IHttpErrorMapper
{
    public string Map(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var reason = response.ReasonPhrase ?? "Unknown";

        var message = statusCode switch
        {
            400 => "400 Bad Request – Invalid request.",
            401 => "401 Unauthorized – Invalid API key or authentication failed.",
            403 => "403 Forbidden – Access to the resource is restricted.",
            404 => "404 Not Found – The requested resource could not be found.",
            429 => "429 Too Many Requests – Rate limit exceeded.",
            500 => "500 Internal Server Error – Server encountered an error.",
            503 => "503 Service Unavailable – Service is temporarily down.",
            _   => $"{statusCode} {reason} – Unexpected error."
        };

        logger.LogError("GeoLite download failed: {Message}", message);
        return $"Error: {message}";
    }
}