namespace DataGateMonitor.Services.Cache;

public interface IStatusCacheGenerationService
{
    string CurrentStamp { get; }

    long Bump();
}
