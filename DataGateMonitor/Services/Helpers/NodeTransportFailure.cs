using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;

namespace DataGateMonitor.Services.Helpers;

/// <summary>
/// Recognises network-level failures when talking to a VPN node (connection refused/reset, TLS handshake
/// aborted, DNS failure) and turns them into a one-line description. Such failures are expected
/// operational noise, so callers log the description instead of a multi-line stack trace.
/// </summary>
public static class NodeTransportFailure
{
    public static bool TryDescribe(Exception ex, out string description)
    {
        description = string.Empty;

        var http = FindInChain<HttpRequestException>(ex);
        if (http == null)
            return false;

        var socket = FindInChain<SocketException>(http);
        var isTransport = http.HttpRequestError is HttpRequestError.ConnectionError
                              or HttpRequestError.SecureConnectionError
                              or HttpRequestError.NameResolutionError
                              or HttpRequestError.ProxyTunnelError
                          || socket != null
                          || FindInChain<IOException>(http) != null
                          || FindInChain<AuthenticationException>(http) != null;
        if (!isTransport)
            return false;

        var stage = http.HttpRequestError switch
        {
            HttpRequestError.SecureConnectionError => "TLS handshake failed",
            HttpRequestError.NameResolutionError => "DNS resolution failed",
            HttpRequestError.ProxyTunnelError => "Proxy tunnel failed",
            _ => "Connection failed"
        };

        var reason = socket != null
            ? $"{socket.Message} (socket error {socket.SocketErrorCode})"
            : Innermost(http).Message;

        description = $"{stage}: {reason}";
        return true;
    }

    private static T? FindInChain<T>(Exception? ex) where T : Exception
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is T match)
                return match;
        }

        return null;
    }

    private static Exception Innermost(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current;
    }
}
