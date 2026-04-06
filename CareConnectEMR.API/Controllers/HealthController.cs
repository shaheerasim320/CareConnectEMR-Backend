using Microsoft.AspNetCore.Mvc;

namespace CareConnectEMR.API.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "CareConnect EMR API",
                version = "1.0.0",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
