namespace OpenVPNGateMonitor.Services.GeoLite.Interfaces;

public interface IHttpErrorMapper
{
    string Map(HttpResponseMessage response);
}