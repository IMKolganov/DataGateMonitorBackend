using System.Text;
using System.IO.Compression;
using OpenVPNGateMonitor.Services.Helpers;

namespace OpenVPNGateMonitor.Tests.Services.Helpers;

public class GZipTests
{
    [Fact]
    public async Task ExtractTarGzAsync_ExtractsSingleFileCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "gzip-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var tarGzPath = Path.Combine(tempDir, "test.tar.gz");
        var fileName = "folder/file.txt";
        var fileContent = "Hello from tar!";

        await CreateTarGzAsync(tarGzPath, (fileName, fileContent, (byte)'0'));

        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(outputDir);

        await GZip.ExtractTarGzAsync(tarGzPath, outputDir, null, CancellationToken.None);

        var extractedFile = Path.Combine(outputDir, "folder", "file.txt");
        Assert.True(File.Exists(extractedFile));

        var content = await File.ReadAllTextAsync(extractedFile);
        Assert.Equal(fileContent, content);

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task ExtractTarGzAsync_ReportsProgress_UpTo100()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "gzip-test-progress-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var tarGzPath = Path.Combine(tempDir, "test-progress.tar.gz");

        // Make archive with several files to have some progress steps
        await CreateTarGzAsync(
            tarGzPath,
            ("file1.txt", new string('A', 1024), (byte)'0'),
            ("file2.txt", new string('B', 2048), (byte)'0'),
            ("dir/file3.txt", new string('C', 4096), (byte)'0'));

        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(outputDir);

        var progressValues = new List<int>();

        await GZip.ExtractTarGzAsync(
            tarGzPath,
            outputDir,
            percent =>
            {
                progressValues.Add(percent);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.NotEmpty(progressValues);
        Assert.Equal(100, progressValues[^1]);

        // Ensure progress is between 0 and 100 and non-decreasing
        int previous = -1;
        foreach (var p in progressValues)
        {
            Assert.InRange(p, 0, 100);
            Assert.True(p >= previous);
            previous = p;
        }

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task ExtractTarGzAsync_SkipsLinkEntries()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "gzip-test-link-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var tarGzPath = Path.Combine(tempDir, "test-link.tar.gz");

        // One "link" entry (type '1') and one regular file
        await CreateTarGzAsync(
            tarGzPath,
            ("link-target", string.Empty, (byte)'1'),
            ("real-file.txt", "Real content", (byte)'0'));

        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(outputDir);

        await GZip.ExtractTarGzAsync(tarGzPath, outputDir, null, CancellationToken.None);

        // Link should not be materialized; only real-file.txt should exist
        var realFile = Path.Combine(outputDir, "real-file.txt");
        Assert.True(File.Exists(realFile));

        var linkFile = Path.Combine(outputDir, "link-target");
        Assert.False(File.Exists(linkFile));

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task ExtractTarGzAsync_PreventsPathTraversal()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "gzip-test-traversal-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var tarGzPath = Path.Combine(tempDir, "test-traversal.tar.gz");

        // Attempt to write outside output directory
        await CreateTarGzAsync(
            tarGzPath,
            ("../evil.txt", "should-not-be-written", (byte)'0'));

        var outputDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(outputDir);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await GZip.ExtractTarGzAsync(tarGzPath, outputDir, null, CancellationToken.None);
        });

        var evilPath = Path.GetFullPath(Path.Combine(outputDir, "..", "evil.txt"));
        Assert.False(File.Exists(evilPath));

        Directory.Delete(tempDir, recursive: true);
    }

    // --------- helpers ---------

    private static async Task CreateTarGzAsync(
        string tarGzPath,
        params (string Name, string Content, byte TypeFlag)[] entries)
    {
        await using var tarStream = new MemoryStream();
        using (var writer = new BinaryWriter(tarStream, Encoding.ASCII, leaveOpen: true))
        {
            foreach (var (name, content, typeFlag) in entries)
            {
                WriteTarHeader(writer, name, content.Length, typeFlag);
                if (typeFlag == (byte)'0')
                {
                    var bytes = Encoding.UTF8.GetBytes(content);
                    writer.Write(bytes);

                    var padding = (512 - (bytes.Length % 512)) % 512;
                    if (padding > 0)
                    {
                        writer.Write(new byte[padding]);
                    }
                }
            }

            // Two zero blocks to terminate tar
            writer.Write(new byte[512]);
            writer.Write(new byte[512]);
        }

        tarStream.Position = 0;

        await using var fileStream = new FileStream(tarGzPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var gzip = new GZipStream(fileStream, CompressionMode.Compress);
        await tarStream.CopyToAsync(gzip);
    }

    private static void WriteTarHeader(BinaryWriter writer, string name, int size, byte typeFlag)
    {
        var header = new byte[512];

        // name (0-99)
        WriteString(header, 0, 100, name);

        // mode, uid, gid (leave zeros or simple values)
        WriteOctal(header, 100, 8, 0); // mode
        WriteOctal(header, 108, 8, 0); // uid
        WriteOctal(header, 116, 8, 0); // gid

        // size (124-135)
        WriteOctal(header, 124, 12, size);

        // mtime
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        WriteOctal(header, 136, 12, unixTime);

        // checksum placeholder: spaces
        for (int i = 148; i < 156; i++)
            header[i] = 0x20;

        // typeflag
        header[156] = typeFlag;

        // magic "ustar" (optional, but fine)
        WriteString(header, 257, 6, "ustar");
        WriteString(header, 263, 2, "00");

        // compute checksum
        long sum = 0;
        foreach (var b in header)
            sum += b;

        WriteOctal(header, 148, 6, sum);
        header[154] = 0;       // null
        header[155] = 0x20;    // space

        writer.Write(header);
    }

    private static void WriteString(byte[] buffer, int offset, int length, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        var count = Math.Min(bytes.Length, length);
        Array.Copy(bytes, 0, buffer, offset, count);
        // rest already zero
    }

    private static void WriteOctal(byte[] buffer, int offset, int length, long value)
    {
        var octal = Convert.ToString(value, 8) ?? "0";
        var str = octal.PadLeft(length - 1, '0'); // last char is null
        var bytes = Encoding.ASCII.GetBytes(str);
        Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        buffer[offset + length - 1] = 0; // null terminator
    }
}
