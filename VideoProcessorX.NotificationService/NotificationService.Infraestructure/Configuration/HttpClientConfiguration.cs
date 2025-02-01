using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace NotificationService.Infraestructure.Configuration
{
    public static class HttpClientPolicyExtensions
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
            ResiliencePolicyConfig config,
            ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    config.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(config.RetryBaseDelaySeconds, retryAttempt)),
                    onRetry: (outcome, delay, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Retentativa {RetryCount} para {RequestKey} - Erro: {ErrorMessage}",
                            retryCount,
                            context.PolicyKey,
                            outcome.Exception?.Message);
                    });
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
            ResiliencePolicyConfig config,
            ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    config.CircuitBreakerThreshold,
                    TimeSpan.FromSeconds(config.CircuitBreakerDurationSeconds),
                    onBreak: (outcome, breakDelay, context) =>
                    {
                        logger.LogError(
                            "Circuito aberto por {BreakDelay}s - Último erro: {ErrorMessage}",
                            breakDelay.TotalSeconds,
                            outcome.Exception?.Message);
                    },
                    onReset: context =>
                    {
                        logger.LogInformation("Circuito fechado");
                    });
        }
    }
}
