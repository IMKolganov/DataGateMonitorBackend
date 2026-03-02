using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Tags;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(ITagService tagService) : BaseController
{
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<TagsResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await tagService.GetAllAsync(ct);
        var response = new TagsResponse { Tags = entities.Adapt<List<TagDto>>() };
        return Ok(ApiResponse<TagsResponse>.SuccessResponse(response));
    }

    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<ApiResponse<TagResponse>>> GetById(int id, CancellationToken ct)
    {
        var entity = await tagService.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound(ApiResponse<TagResponse>.ErrorResponse("Tag not found"));
        var response = new TagResponse { Tag = entity.Adapt<TagDto>() };
        return Ok(ApiResponse<TagResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<TagResponse>>> Create(
        [FromBody] CreateOrUpdateTagRequest request,
        CancellationToken ct)
    {
        var created = await tagService.CreateAsync(request.Name, ct);
        var response = new TagResponse { Tag = created.Adapt<TagDto>() };
        return Ok(ApiResponse<TagResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpPut("update/{id:int}")]
    public async Task<ActionResult<ApiResponse<TagResponse>>> Update(
        int id,
        [FromBody] CreateOrUpdateTagRequest request,
        CancellationToken ct)
    {
        await tagService.UpdateAsync(id, request.Name, ct);
        var entity = await tagService.GetByIdAsync(id, ct);
        var response = new TagResponse { Tag = entity!.Adapt<TagDto>() };
        return Ok(ApiResponse<TagResponse>.SuccessResponse(response));
    }

    [Authorize(Roles = "Admin,App")]
    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken ct)
    {
        await tagService.DeleteAsync(id, ct);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }
}
