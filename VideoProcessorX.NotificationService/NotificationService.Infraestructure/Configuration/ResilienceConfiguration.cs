namespace NotificationService.Infraestructure.Configuration
{
    public class ResiliencePolicyConfig
    {
        public int RetryCount { get; set; }
        public int RetryBaseDelaySeconds { get; set; }
        public int CircuitBreakerThreshold { get; set; }
        public int CircuitBreakerDurationSeconds { get; set; }
    }
}
