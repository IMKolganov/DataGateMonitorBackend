namespace DataGateMonitor.Services.Helpers.Interfaces;

public interface IExternalIpAddressService
{
    Task<string> GetRemoteIpAddress(CancellationToken cancellationToken);
}