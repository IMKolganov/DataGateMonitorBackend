using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    public BaseController()
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public ActionResult<ApiResponse<string>> Healthcheck()
    {
        return Ok(ApiResponse<string>.SuccessResponse("Ok"));
    }
    
    [HttpGet("HealthcheckWithJwt")]
    [Authorize]
    public ActionResult<ApiResponse<string>> HealthcheckWithJwt()
    {
        return Ok(ApiResponse<string>.SuccessResponse("Healthy" ));
    }
}