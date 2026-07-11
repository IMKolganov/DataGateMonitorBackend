using DataGateMonitor.DataBase.Services.Query;

namespace DataGateMonitor.Tests.DataBase.Services.Query;

public class GridFilterHelperTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  bob  ", "bob")]
    public void Normalize_Trims_And_Rejects_Blank(string? input, string? expected)
    {
        Assert.Equal(expected, GridFilterHelper.Normalize(input));
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("100%", "100\\%")]
    [InlineData("a_b", "a\\_b")]
    [InlineData(@"a\b", @"a\\b")]
    public void EscapeIlikeLiteral_Escapes_Metacharacters(string input, string expected)
    {
        Assert.Equal(expected, GridFilterHelper.EscapeIlikeLiteral(input));
    }

    [Fact]
    public void ContainsPattern_Wraps_Escaped_Value()
    {
        Assert.Equal("%100\\%%", GridFilterHelper.ContainsPattern("100%"));
    }

    [Fact]
    public void ExactMatchPattern_Does_Not_Add_Wildcards()
    {
        Assert.Equal("ClientConnect", GridFilterHelper.ExactMatchPattern("ClientConnect"));
        Assert.Equal("Client\\%Connect", GridFilterHelper.ExactMatchPattern("Client%Connect"));
    }
}
