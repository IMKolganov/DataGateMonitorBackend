using DataGateMonitor.Models.EmailTemplates;
using Xunit;

namespace DataGateMonitor.Tests.EmailTemplates;

public class TransactionalEmailHtmlTests
{
    [Fact]
    public void EnsureConfirmEmailAction_WhenUrlAlreadyPresent_ReturnsUnchanged()
    {
        var html = TransactionalEmailHtml.BuildEmailConfirmationWithPlaceholders();

        var result = TransactionalEmailHtml.EnsureConfirmEmailAction(html);

        Assert.Equal(html, result);
        Assert.Contains(TransactionalEmailHtml.DefaultConfirmEmailPageUrl, result);
    }

    [Fact]
    public void EnsureConfirmEmailAction_WhenMissing_InsertsButtonBeforeCodePanel()
    {
        const string templateWithoutButton =
            """
            <div class="code-panel" style="margin:18px 0 12px;">
              <div>{{CODE}}</div>
            </div>
            """;

        var result = TransactionalEmailHtml.EnsureConfirmEmailAction(templateWithoutButton);

        Assert.Contains(TransactionalEmailHtml.DefaultConfirmEmailPageUrl, result);
        Assert.Contains("Enter confirmation code", result);
        Assert.StartsWith(
            """
            <div class="btn-wrap">
            """,
            result[..result.IndexOf("<div class=\"code-panel\"", StringComparison.Ordinal)]);
    }

    [Fact]
    public void EnsureConfirmEmailAction_WhenNoCodePanel_ReturnsUnchanged()
    {
        const string html = "<p>No panel here</p>";

        var result = TransactionalEmailHtml.EnsureConfirmEmailAction(html);

        Assert.Equal(html, result);
    }
}
