using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("NotificationService is running");
    }
}
