using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Mapping.Tags.Mappings;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Tags;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class TagsControllerTests
{
    private readonly Mock<ITagService> _service = new();
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(TagMapping).Assembly);
        _controller = new TagsController(_service.Object);
    }

    [Fact]
    public async Task GetAll_Returns_Ok_WithTags()
    {
        var tags = new List<Tag> { new() { Id = 1, Name = "A" }, new() { Id = 2, Name = "B" } };
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tags);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TagsResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Tags.Count);
        Assert.Equal("A", response.Data.Tags[0].Name);
        Assert.Equal("B", response.Data.Tags[1].Name);
        _service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenFound_Returns_Ok()
    {
        var tag = new Tag { Id = 5, Name = "X" };
        _service.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var result = await _controller.GetById(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TagResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Tag);
        Assert.Equal(5, response.Data.Tag.Id);
        Assert.Equal("X", response.Data.Tag.Name);
        _service.Verify(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns_NotFound()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Tag?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        _service.Verify(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Returns_Ok_WithCreatedTag()
    {
        var created = new Tag { Id = 10, Name = "new" };
        _service.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var request = new CreateOrUpdateTagRequest { Name = "new" };
        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TagResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Tag);
        Assert.Equal(10, response.Data.Tag.Id);
        Assert.Equal("new", response.Data.Tag.Name);
        _service.Verify(s => s.CreateAsync("new", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_Returns_Ok_WithUpdatedTag()
    {
        var updated = new Tag { Id = 3, Name = "updated" };
        _service.Setup(s => s.UpdateAsync(3, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _service.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var request = new CreateOrUpdateTagRequest { Name = "updated" };
        var result = await _controller.Update(3, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TagResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Tag);
        Assert.Equal("updated", response.Data.Tag.Name);
        _service.Verify(s => s.UpdateAsync(3, "updated", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Returns_Ok_WithTrue()
    {
        _service.Setup(s => s.DeleteAsync(7, It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var result = await _controller.Delete(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        _service.Verify(s => s.DeleteAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
