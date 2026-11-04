using System.Net.Http;
using System.Net.Sockets;
using DataGateMonitor.Services.Helpers;

namespace DataGateMonitor.Tests.Services.Helpers;

public class NodeTransportFailureTests
{
    [Fact]
    public void TlsHandshakeReset_IsDescribedAsOneLine()
    {
        var socket = new SocketException((int)SocketError.ConnectionReset);
        var io = new IOException("Unable to read data from the transport connection: Connection reset by peer.", socket);
        var ex = new HttpRequestException(HttpRequestError.SecureConnectionError,
            "The SSL connection could not be established, see inner exception.", io);

        Assert.True(NodeTransportFailure.TryDescribe(ex, out var description));
        Assert.StartsWith("TLS handshake failed: ", description);
        Assert.Contains("ConnectionReset", description);
        Assert.DoesNotContain("\n", description);
    }

    [Fact]
    public void WrappedConnectionRefused_IsRecognised()
    {
        var socket = new SocketException((int)SocketError.ConnectionRefused);
        var http = new HttpRequestException(HttpRequestError.ConnectionError, "Connection refused", socket);
        var ex = new InvalidOperationException("poll failed", http);

        Assert.True(NodeTransportFailure.TryDescribe(ex, out var description));
        Assert.StartsWith("Connection failed: ", description);
        Assert.Contains("ConnectionRefused", description);
    }

    [Fact]
    public void HttpStatusError_IsNotTransportFailure()
    {
        var ex = new HttpRequestException("Response status code does not indicate success: 500.");

        Assert.False(NodeTransportFailure.TryDescribe(ex, out _));
    }

    [Fact]
    public void NonHttpException_IsNotTransportFailure()
    {
        Assert.False(NodeTransportFailure.TryDescribe(new InvalidOperationException("boom"), out _));
    }
}
