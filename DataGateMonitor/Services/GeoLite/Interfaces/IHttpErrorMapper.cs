namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IHttpErrorMapper
{
    string Map(HttpResponseMessage response);
}