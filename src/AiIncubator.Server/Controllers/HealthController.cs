using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;

namespace AiIncubator.Server.Controllers;

[ApiController]
[Route("api/health/")]
[ControllerName(name: "health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    /// <summary>Liveness probe. Used by the client to detect whether the API is reachable.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ApiResponse<object> Get()
    {
        return ApiResponse<object>.Ok(new { status = "ok" });
    }
}
