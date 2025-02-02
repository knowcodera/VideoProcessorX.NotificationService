using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using Polly;
using Polly.CircuitBreaker;

namespace NotificationService.Infraestructure.Email
{
    public class FluentEmailSender : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly ILogger<FluentEmailSender> _logger;

        // Políticas de resiliência (singleton por instância)
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private readonly AsyncPolicy _retryPolicy;

        public FluentEmailSender(
            IFluentEmail fluentEmail,
            ILogger<FluentEmailSender> logger)
        {
            _fluentEmail = fluentEmail;
            _logger = logger;

            // Configuração das políticas
            _retryPolicy = CreateRetryPolicy();
            _circuitBreaker = CreateCircuitBreaker();
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var combinedPolicy = Policy.WrapAsync(_retryPolicy, _circuitBreaker);

            try
            {
                await combinedPolicy.ExecuteAsync(async () =>
                {
                    var response = await _fluentEmail
                        .To(to)
                        .Subject(subject)
                        .Body(body, isHtml: false)
                        .SendAsync();

                    if (!response.Successful)
                    {
                        var errorDetails = string.Join(" | ", response.ErrorMessages);
                        _logger.LogError("Falha no envio para {Email}. Erros: {Errors}", to, errorDetails);
                        throw new EmailSendFailedException($"Falha ao enviar e-mail para {to}");
                    }

                    _logger.LogInformation("E-mail enviado com sucesso para {Email} [ID: {MessageId}]",
                        to, response.MessageId);

                    return response;
                });
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogCritical(ex, "Circuito aberto! Não é possível enviar e-mails temporariamente");
                throw;
            }
            catch (EmailSendFailedException ex)
            {
                _logger.LogError(ex, "Falha permanente ao enviar e-mail para {Email}", to);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar e-mail para {Email}", to);
                throw;
            }
        }

        private AsyncPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Tentativa de reenvio {RetryCount} para {Email}. Erro: {ErrorMessage}",
                            retryCount,
                            context["email"],
                            exception.Message);
                    });
        }

        private AsyncCircuitBreakerPolicy CreateCircuitBreaker()
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogCritical(
                            "Circuito aberto por {BreakDelay} devido a falhas consecutivas. Último erro: {ErrorMessage}",
                            breakDelay,
                            ex.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuito fechado - pronto para tentar novamente");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogWarning("Circuito em modo half-open - testando capacidade de recuperação");
                    });
        }
    }

    public class EmailSendFailedException : Exception
    {
        public EmailSendFailedException(string message) : base(message) { }
        public EmailSendFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
