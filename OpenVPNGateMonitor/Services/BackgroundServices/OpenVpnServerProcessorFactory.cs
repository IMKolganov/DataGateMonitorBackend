using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerProcessorFactory(IServiceProvider serviceProvider)
{
    private readonly Dictionary<int, OpenVpnServerProcessor> _processors = new();
    private readonly object _lock = new();

    public OpenVpnServerProcessor GetOrCreateProcessor(OpenVpnServer server)
    {
        lock (_lock)
        {
            if (!_processors.TryGetValue(server.Id, out var processor))
            {
                processor = ActivatorUtilities.CreateInstance<OpenVpnServerProcessor>(serviceProvider);
                _processors[server.Id] = processor;
            }

            return processor;
        }
    }
}