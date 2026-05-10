using Microsoft.AspNetCore.Mvc;

namespace PayderPay.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            UtcNow = DateTime.UtcNow
        });
    }
}
