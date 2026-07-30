using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health/live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Healthy" });
    }

    [HttpGet("/health/ready")]
    public IActionResult Ready()
    {
        return Ok(new { status = "Healthy" });
    }
}
