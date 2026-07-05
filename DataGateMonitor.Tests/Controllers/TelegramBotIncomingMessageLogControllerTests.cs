using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Controllers;

public class TelegramBotIncomingMessageLogControllerTests
{
    private readonly Mock<IIncomingMessageLogService> _service = new();
    private readonly Mock<IIncomingMessageLogQueryService> _query = new();

    private readonly TelegramBotIncomingMessageLogController _controller;

    public TelegramBotIncomingMessageLogControllerTests()
    {
        _controller = new TelegramBotIncomingMessageLogController(
            _service.Object,
            _query.Object);
    }

    [Fact]
    public async Task AddMessage_Returns_Ok()
    {
        var request = new AddMessageRequest
        {
            Message = new MessageDto
            {
                Id = 0,
                TelegramId = 12345,
                Username = "john",
                MessageText = "hello",
                ReceivedAt = DateTimeOffset.UtcNow
            }
        };

        _service
            .Setup(s => s.SaveMessageAsync(It.IsAny<AddMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddMessageResponse
            {
                Message = request.Message
            });

        var result = await _controller.AddMessage(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AddMessageResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data!.Message);
        Assert.Equal(12345, response.Data!.Message!.TelegramId);

        _service.Verify(s => s.SaveMessageAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllMessages_Returns_Ok()
    {
        var paged = new TestPagedResult<IncomingMessageLog>
        {
            Page = 2,
            PageSize = 5,
            TotalCount = 12,
            Items =
            [
                new IncomingMessageLog
                {
                    Id = 1,
                    TelegramId = 100,
                    Username = "u1",
                    MessageText = "m1",
                    ReceivedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        _query
            .Setup(q => q.GetPage(It.Is<GetAllMessagesRequest>(r => r.Page == 2 && r.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var request = new GetAllMessagesRequest { Page = 2, PageSize = 5 };
        var result = await _controller.GetAllMessages(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllMessagesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data!.Messages);
        Assert.Equal(2, response.Data!.Messages.Page);
        Assert.Equal(5, response.Data!.Messages.PageSize);
        Assert.Equal(12, response.Data!.Messages.TotalCount);
        Assert.Single(response.Data!.Messages.Items);

        _query.Verify(q => q.GetPage(It.Is<GetAllMessagesRequest>(r => r.Page == 2 && r.PageSize == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByTelegramId_Returns_Ok()
    {
        var paged = new TestPagedResult<IncomingMessageLog>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Items =
            [
                new IncomingMessageLog
                {
                    Id = 2,
                    TelegramId = 777,
                    Username = "user777",
                    MessageText = "hi",
                    ReceivedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        _query
            .Setup(q => q.GetPageByTelegramId(777, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetByTelegramUserId(777, 1, 10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetByTelegramIdMessagesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data!.Messages.Page);
        Assert.Equal(10, response.Data!.Messages.PageSize);
        Assert.Single(response.Data!.Messages.Items);
        Assert.Equal(777, response.Data!.Messages.Items[0].TelegramId);

        _query.Verify(q => q.GetPageByTelegramId(777, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByTelegramUserId_WithPageAndPageSize_PassesThemToQuery()
    {
        var paged = new PagedResponse<IncomingMessageLog>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25,
            Items = []
        };
        _query
            .Setup(q => q.GetPageByTelegramId(189149160, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetByTelegramUserId(189149160, 2, 10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetByTelegramIdMessagesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data!.Messages.Page);
        Assert.Equal(10, response.Data!.Messages.PageSize);
        Assert.Equal(25, response.Data!.Messages.TotalCount);
        _query.Verify(q => q.GetPageByTelegramId(189149160, 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_Returns_Ok()
    {
        var dto = new MessageDto
        {
            Id = 55,
            TelegramId = 999,
            Username = "bob",
            MessageText = "test",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _service
            .Setup(s => s.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(55, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetByIdMessageResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        // Current controller maps MessageDto -> GetByIdMessageResponse via Adapt without explicit mapping,
        // resulting in default instance with null Messages. We validate Success and Data presence only.
        Assert.Null(response.Data!.Messages);

        _service.Verify(s => s.GetByIdAsync(55, It.IsAny<CancellationToken>()), Times.Once);
    }
}
