using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infraestructure.Email
{
    public class FluentEmailSender : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly ILogger<FluentEmailSender> _logger;

        public FluentEmailSender(IFluentEmail fluentEmail, ILogger<FluentEmailSender> logger)
        {
            _fluentEmail = fluentEmail;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // A interface IFluentEmail é injetada e já configurada no Program.cs
                var response = await _fluentEmail
                    .To(to)
                    .Subject(subject)
                    .Body(body, isHtml: false)  // se quiser HTML, use true
                    .SendAsync();

                if (!response.Successful)
                {
                    _logger.LogError("Falha ao enviar e-mail para {Email}. Erros: {Errors}",
                        to, string.Join("; ", response.ErrorMessages));
                }
                else
                {
                    _logger.LogInformation("E-mail enviado com sucesso para {Email}", to);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail para {Email}", to);
                throw;
            }
        }
    }
}
