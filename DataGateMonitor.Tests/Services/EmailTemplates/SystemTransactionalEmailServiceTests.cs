using System.Linq.Expressions;
using Moq;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;
using DataGateMonitor.Models.EmailTemplates;
using DataGateMonitor.Services.EmailTemplates;

namespace DataGateMonitor.Tests.Services.EmailTemplates;

public class SystemTransactionalEmailServiceTests
{
    private readonly Mock<IQueryService<EmailBroadcastTemplate, int>> _templateQuery = new();

    private SystemTransactionalEmailService CreateSut() => new(_templateQuery.Object);

    private void SetupTemplate(EmailBroadcastTemplate? template)
    {
        _templateQuery
            .Setup(q => q.FirstOrDefault(
                It.IsAny<Expression<Func<EmailBroadcastTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<EmailBroadcastTemplate>, IOrderedQueryable<EmailBroadcastTemplate>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<EmailBroadcastTemplate, object>>[]>()))
            .ReturnsAsync(template);
    }

    [Fact]
    public async Task GetFreeTierGraceDisconnectedAsync_WhenNoDbTemplate_UsesHardcodedFallback()
    {
        SetupTemplate(null);
        var sut = CreateSut();

        var (subject, bodyHtml) = await sut.GetFreeTierGraceDisconnectedAsync(
            "Free", "@DataGateVPNBot", CancellationToken.None);

        Assert.Equal(TransactionalEmailHtml.DefaultFreeTierGraceDisconnectedSubject, subject);
        Assert.Contains("Free", bodyHtml);
        Assert.Contains("@DataGateVPNBot", bodyHtml);
    }

    [Fact]
    public async Task GetFreeTierGraceDisconnectedAsync_WhenDbTemplateExists_AppliesPlaceholders()
    {
        SetupTemplate(new EmailBroadcastTemplate
        {
            Name = SystemEmailTemplateNames.FreeTierGraceDisconnected,
            Subject = "Custom subject",
            BodyHtml = "<p>Plan: {{PLAN_NAME}}, Channel: {{REQUIRED_CHANNEL}}</p>",
        });
        var sut = CreateSut();

        var (subject, bodyHtml) = await sut.GetFreeTierGraceDisconnectedAsync(
            "Default", "@DataGateVPNBot", CancellationToken.None);

        Assert.Equal("Custom subject", subject);
        Assert.Equal("<p>Plan: Default, Channel: @DataGateVPNBot</p>", bodyHtml);
    }

    [Fact]
    public async Task GetFreeTierGraceDisconnectedAsync_WhenDbTemplateHasBlankSubject_UsesDefaultSubject()
    {
        SetupTemplate(new EmailBroadcastTemplate
        {
            Name = SystemEmailTemplateNames.FreeTierGraceDisconnected,
            Subject = "   ",
            BodyHtml = "<p>{{PLAN_NAME}}</p>",
        });
        var sut = CreateSut();

        var (subject, _) = await sut.GetFreeTierGraceDisconnectedAsync("Free", "@DataGateVPNBot", CancellationToken.None);

        Assert.Equal(TransactionalEmailHtml.DefaultFreeTierGraceDisconnectedSubject, subject);
    }
}
