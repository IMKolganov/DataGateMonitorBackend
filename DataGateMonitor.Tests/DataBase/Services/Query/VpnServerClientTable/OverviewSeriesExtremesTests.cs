using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OverviewSeriesExtremesTests
{
    [Fact]
    public void Compute_Empty_Returns_Null_Extremes()
    {
        var result = OverviewSeriesExtremes.Compute(Array.Empty<(DateTimeOffset, int)>());

        Assert.Null(result.Peak);
        Assert.Null(result.Low);
    }

    [Fact]
    public void Compute_Single_Point_Is_Both_Peak_And_Low()
    {
        var ts = new DateTimeOffset(2026, 9, 28, 3, 0, 0, TimeSpan.Zero);
        var result = OverviewSeriesExtremes.Compute([(ts, 12)]);

        Assert.Equal(12, result.Peak!.Value.Count);
        Assert.Equal(ts, result.Peak.Value.At);
        Assert.Equal(12, result.Low!.Value.Count);
        Assert.Equal(ts, result.Low.Value.At);
    }

    [Fact]
    public void Compute_Peak_And_Low_Keep_Earliest_Timestamp_On_Ties()
    {
        var t0 = new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);
        var t1 = t0.AddHours(1);
        var t2 = t0.AddHours(2);
        var t3 = t0.AddHours(3);

        // Mirrors prod dump pattern: unique-over-day >> concurrent peak/low.
        var result = OverviewSeriesExtremes.Compute(
        [
            (t0, 15),
            (t1, 38),
            (t2, 38), // same peak later — keep earliest
            (t3, 15), // same low later — keep earliest
        ]);

        Assert.Equal(38, result.Peak!.Value.Count);
        Assert.Equal(t1, result.Peak.Value.At);
        Assert.Equal(15, result.Low!.Value.Count);
        Assert.Equal(t0, result.Low.Value.At);
    }

    [Fact]
    public void Compute_Ignores_WouldBe_Zero_Fill_When_Not_In_Input()
    {
        // Frontend/old summary took Min after FillMissingBuckets → Low=0.
        // Sampling-only input must keep the real overnight trough.
        var night = new DateTimeOffset(2026, 9, 28, 3, 0, 0, TimeSpan.Zero);
        var day = night.AddHours(12);

        var result = OverviewSeriesExtremes.Compute(
        [
            (night, 11),
            (day, 40),
        ]);

        Assert.Equal(40, result.Peak!.Value.Count);
        Assert.Equal(day, result.Peak.Value.At);
        Assert.Equal(11, result.Low!.Value.Count);
        Assert.Equal(night, result.Low.Value.At);
    }

    [Fact]
    public void Compute_Sampled_Zero_Is_Valid_Low()
    {
        var t0 = new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);
        var t1 = t0.AddHours(1);

        var result = OverviewSeriesExtremes.Compute(
        [
            (t0, 0),
            (t1, 12),
        ]);

        Assert.Equal(12, result.Peak!.Value.Count);
        Assert.Equal(0, result.Low!.Value.Count);
        Assert.Equal(t0, result.Low.Value.At);
    }
}
