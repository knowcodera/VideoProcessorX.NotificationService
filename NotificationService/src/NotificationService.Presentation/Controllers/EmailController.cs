using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly INotificationEmailService _notificationService;

        public EmailController(INotificationEmailService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] EmailRequestDto request)
        {
            await _notificationService.NotifyAsync(request.To, request.Subject, request.Body);
            return Ok();
        }
    }
}
