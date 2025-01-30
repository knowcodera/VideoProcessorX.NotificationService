using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailSender _emailSender;

        public EmailController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        /// <summary>
        /// Endpoint para enviar um e-mail de teste via POST /api/email/test
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] EmailRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.To))
                return BadRequest("O campo 'To' é obrigatório.");

            try
            {
                await _emailSender.SendEmailAsync(request.To, request.Subject, request.Body);
                return Ok("E-mail enviado com sucesso!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao enviar e-mail: {ex.Message}");
            }
        }
    }
}
