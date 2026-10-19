using System.Runtime.InteropServices;

namespace DataGateMonitor.Services.GeoLite;

/// <summary>Detects disk-full / ENOSPC failures across platforms.</summary>
public static class GeoLiteDiskFull
{
    public static bool IsDiskFull(Exception? ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is IOException io)
            {
                if (ContainsNoSpaceMessage(io.Message))
                    return true;

                // Windows ERROR_DISK_FULL / ERROR_HANDLE_DISK_FULL
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && io.HResult is unchecked((int)0x80070070) or unchecked((int)0x80070027))
                    return true;

                // Linux ENOSPC often surfaces as HResult 0x80070070 as well, or message text.
            }

            if (ContainsNoSpaceMessage(current.Message))
                return true;
        }

        return false;
    }

    public static string FormatDetail(Exception ex)
        => IsDiskFull(ex)
            ? $"No space left on device. Free disk space under /app/resources (GeoLite volume) and prune old GeoLite2_* folders. {ex.GetType().Name}: {ex.Message}"
            : $"{ex.GetType().Name}: {ex.Message}";

    private static bool ContainsNoSpaceMessage(string? message)
        => !string.IsNullOrEmpty(message)
           && (message.Contains("No space left on device", StringComparison.OrdinalIgnoreCase)
               || message.Contains("There is not enough space on the disk", StringComparison.OrdinalIgnoreCase)
               || message.Contains("ENOSPC", StringComparison.OrdinalIgnoreCase));
}
