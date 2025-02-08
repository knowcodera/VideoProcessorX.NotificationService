using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Domain.DTOs;
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

        /// <summary>
        /// Envia o e-mail usando FluentEmail com política de retry + circuit breaker
        /// </summary>
        public async Task SendEmailAsync(NotificationMessageDto dto)
        {
            // Wrap das políticas
            var combinedPolicy = Policy.WrapAsync(_retryPolicy, _circuitBreaker);

            // Cria um contexto Polly para logs de retry
            var pollyContext = new Context();
            pollyContext["email"] = dto.Email;

            try
            {
                await combinedPolicy.ExecuteAsync(async (ctx) =>
                {
                    // Monta objeto de e-mail
                    var email = _fluentEmail
                        .To(dto.Email)
                        .Subject(dto.Subject)
                        .Body(dto.Body, isHtml: false);

                    // Se houver AttachmentPath no DTO, anexa o arquivo
                    if (!string.IsNullOrEmpty(dto.AttachmentPath))
                    {
                        byte[] zipBytes = File.ReadAllBytes(dto.AttachmentPath);

                        email.Attach(new Attachment
                        {
                            Data = new MemoryStream(zipBytes),
                            ContentType = "application/zip",
                            Filename = Path.GetFileName(dto.AttachmentPath)
                        });
                    }

                    // Chama FluentEmail para enviar
                    var response = await email.SendAsync();

                    // Verifica se houve falha
                    if (!response.Successful)
                    {
                        var errorDetails = string.Join(" | ", response.ErrorMessages);
                        _logger.LogError("Falha ao enviar e-mail para {Email}. Erros: {Errors}", dto.Email, errorDetails);
                        throw new EmailSendFailedException($"Falha ao enviar e-mail para {dto.Email}");
                    }

                    _logger.LogInformation(
                        "E-mail enviado com sucesso para {Email} [MensagemID: {MessageId}] | Anexo? {HasAttachment}",
                        dto.Email,
                        response.MessageId,
                        string.IsNullOrEmpty(dto.AttachmentPath) ? "Não" : "Sim");

                    return response; // devolve para a policy
                }, pollyContext);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogCritical(ex, "Circuito aberto! Não é possível enviar e-mails temporariamente (Email: {Email})", dto.Email);
                throw;
            }
            catch (EmailSendFailedException ex)
            {
                _logger.LogError(ex, "Falha permanente ao enviar e-mail para {Email}", dto.Email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar e-mail para {Email}", dto.Email);
                throw;
            }
        }

        /// <summary>
        /// Política de retry exponencial (2^N) por 3 tentativas
        /// </summary>
        private AsyncPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, retryCount, context) =>
                    {
                        var email = context.ContainsKey("email") ? context["email"].ToString() : "desconhecido";
                        _logger.LogWarning(
                            "Tentativa de reenvio {RetryCount} para {Email}. Erro: {ErrorMessage}",
                            retryCount,
                            email,
                            exception.Message);
                    });
        }

        /// <summary>
        /// Política de circuit breaker: após 5 falhas consecutivas, aguarda 1 minuto antes de aceitar novas tentativas
        /// </summary>
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
                            "Circuito aberto por {BreakDelay}s devido a falhas consecutivas. Último erro: {ErrorMessage}",
                            breakDelay.TotalSeconds,
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

    /// <summary>
    /// Exceção customizada para falha de envio
    /// </summary>
    public class EmailSendFailedException : Exception
    {
        public EmailSendFailedException(string message) : base(message) { }
        public EmailSendFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
