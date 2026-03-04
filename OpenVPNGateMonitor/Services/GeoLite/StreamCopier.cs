using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class StreamCopier(IGeoLiteProgressNotifier notifier) : IStreamCopier
{
    public async Task CopyWithProgressAsync(
        Stream source,
        Stream destination,
        long totalBytes,
        int currentStep,
        int totalSteps,
        string stepTitle,
        CancellationToken ct)
    {
        // Copies with progress callback (0..100)
        var buffer = new byte[8192];
        long totalRead = 0L;
        int? lastPercentSent = null;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (bytesRead <= 0) break;

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)(totalRead * 100 / totalBytes);
                if (lastPercentSent != percent)
                {
                    lastPercentSent = percent;
                    await notifier.ReportStepAsync(currentStep, totalSteps, stepTitle, percent, ct);
                }
            }
        }

        if (lastPercentSent != 100)
            await notifier.ReportStepAsync(currentStep, totalSteps, stepTitle, 100, ct);
    }
}