namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IStreamCopier
{
    Task CopyWithProgressAsync(
        Stream source,
        Stream destination,
        long totalBytes,
        int currentStep,
        int totalSteps,
        string stepTitle,
        CancellationToken ct);
}