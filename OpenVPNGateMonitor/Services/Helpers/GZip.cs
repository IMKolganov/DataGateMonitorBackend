using System.IO.Compression;
using System.Text;

namespace OpenVPNGateMonitor.Services.Helpers;

public static class GZip
{
    public static async Task ExtractTarGzAsync(
        string filePath,
        string outputDir,
        Func<int, Task>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
        await using var tarStream = new MemoryStream();

        await gzipStream.CopyToAsync(tarStream, cancellationToken);
        tarStream.Position = 0;

        await ExtractTarAsync(tarStream, outputDir, tarStream.Length, reportProgress, cancellationToken);
    }

    private static async Task ExtractTarAsync(
        Stream tarStream,
        string outputDir,
        long totalBytes,
        Func<int, Task>? reportProgress,
        CancellationToken cancellationToken)
    {
        using var reader = new BinaryReader(tarStream, Encoding.ASCII, leaveOpen: true);
        long processedBytes = 0;
        int lastReported = -1;

        while (tarStream.Position < tarStream.Length)
        {
            var header = reader.ReadBytes(512);
            processedBytes += header.Length;

            if (header.All(b => b == 0)) break;

            string name = Encoding.ASCII.GetString(header, 0, 100).Trim('\0');
            if (string.IsNullOrWhiteSpace(name)) break;

            byte typeFlag = header[156];
            if (typeFlag == 49) continue; // skip link

            string sizeString = Encoding.ASCII.GetString(header, 124, 12).Trim('\0').Trim();
            long size = Convert.ToInt64(sizeString, 8);

            string outputFile = Path.GetFullPath(Path.Combine(outputDir, name.Replace('/', Path.DirectorySeparatorChar)));

            if (!outputFile.StartsWith(Path.GetFullPath(outputDir)))
                throw new InvalidOperationException($"Invalid tar path detected: {name}");

            if (name.EndsWith("/") || name.EndsWith("\\"))
            {
                Directory.CreateDirectory(outputFile);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            await using (var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[8192];
                long remaining = size;

                while (remaining > 0)
                {
                    int read = await tarStream.ReadAsync(buffer.AsMemory(0, 
                        (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                    if (read <= 0) break;

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    remaining -= read;
                    processedBytes += read;

                    if (reportProgress is not null && totalBytes > 0)
                    {
                        int percent = (int)(processedBytes * 100 / totalBytes);
                        if (percent != lastReported)
                        {
                            lastReported = percent;
                            await reportProgress(percent);
                        }
                    }
                }
            }

            long padding = (512 - (size % 512)) % 512;
            tarStream.Seek(padding, SeekOrigin.Current);
            processedBytes += padding;

            if (reportProgress is not null && totalBytes > 0)
            {
                int percent = (int)(processedBytes * 100 / totalBytes);
                if (percent != lastReported)
                {
                    lastReported = percent;
                    await reportProgress(percent);
                }
            }
        }

        if (reportProgress is not null && lastReported < 100)
        {
            await reportProgress(100);
        }
    }
}