using DataGateMonitor.Models;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.BackgroundServices;

public class VpnServerProcessorFactory(IServiceProvider serviceProvider)
{
    private readonly Dictionary<(int Id, VpnServerType Type), IVpnServerWorkProcessor> _processors = new();
    private readonly object _lock = new();

    public IVpnServerWorkProcessor GetOrCreateProcessor(VpnServer server)
    {
        var key = (server.Id, server.ServerType);
        lock (_lock)
        {
            if (!_processors.TryGetValue(key, out var processor))
            {
                processor = server.ServerType switch
                {
                    VpnServerType.Xray => ActivatorUtilities.CreateInstance<XrayServerProcessor>(serviceProvider),
                    _ => ActivatorUtilities.CreateInstance<OpenVpnServerProcessor>(serviceProvider),
                };
                _processors[key] = processor;
            }

            return processor;
        }
    }
}
