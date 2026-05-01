using DataGateMonitor.Services.Api.MobileCrashIngest;

namespace DataGateMonitor.Tests.Services.Api.MobileCrashIngest;

public class CrashReportParserTests
{
    private readonly ICrashReportParser _sut = new CrashReportParser();

    [Fact]
    public void Parse_ValidPayload_ReturnsParsedFieldsAndStacktrace()
    {
        var payload = """
                      timestamp_utc=2026-05-01T00:00:00.000Z
                      process=com.imkolganov.datagate.dev
                      thread=main
                      sdk=35
                      device=Pixel 8
                      kind=fatal
                      exception=java.lang.RuntimeException
                      message=boom
                      tag=network

                      java.lang.RuntimeException: boom
                        at com.imkolganov.datagate.MainActivity.onCreate(MainActivity.kt:42)
                      """;

        var result = _sut.Parse(payload);

        Assert.True(result.IsParsed);
        Assert.Equal(DateTimeOffset.Parse("2026-05-01T00:00:00.000Z"), result.TimestampUtc);
        Assert.Equal("com.imkolganov.datagate.dev", result.Process);
        Assert.Equal("main", result.Thread);
        Assert.Equal("35", result.Sdk);
        Assert.Equal("Pixel 8", result.Device);
        Assert.Equal("fatal", result.Kind);
        Assert.Equal("java.lang.RuntimeException", result.Exception);
        Assert.Equal("boom", result.Message);
        Assert.Equal("network", result.Tag);
        Assert.Contains("MainActivity", result.Stacktrace);
    }

    [Fact]
    public void Parse_HeaderHasMalformedLine_ReturnsFailedAndPreservesStacktrace()
    {
        var payload = """
                      process=com.imkolganov.datagate
                      malformed-line-without-separator

                      stack line 1
                      stack line 2
                      """;

        var result = _sut.Parse(payload);

        Assert.False(result.IsParsed);
        Assert.Equal("stack line 1\nstack line 2", result.Stacktrace);
    }

    [Fact]
    public void Parse_NoHeaderSeparator_ReturnsFailed()
    {
        var payload = "process=com.imkolganov.datagate\nthread=main";

        var result = _sut.Parse(payload);

        Assert.False(result.IsParsed);
        Assert.Null(result.Stacktrace);
    }
}
