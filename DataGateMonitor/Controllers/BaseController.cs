using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/base")]
public class BaseController : ControllerBase
{
    public BaseController()
    {
    }

    [HttpGet("healthcheck")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public ActionResult<ApiResponse<string>> Healthcheck()
    {
        return Ok(ApiResponse<string>.SuccessResponse("Ok"));
    }
    
    [HttpGet("healthcheck-with-jwt")]
    [Authorize]
    public ActionResult<ApiResponse<string>> HealthcheckWithJwt()
    {
        return Ok(ApiResponse<string>.SuccessResponse("Healthy" ));
    }
}