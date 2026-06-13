using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.DataBase.UnitOfWork;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

internal static class OverviewQueryTestHelper
{
    public static IOverviewTrafficAggregator CreateTrafficAggregator(IUnitOfWork uow)
        => new OverviewTrafficAggregator(uow);
}
