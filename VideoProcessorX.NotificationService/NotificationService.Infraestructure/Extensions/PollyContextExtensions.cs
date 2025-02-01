using Microsoft.Extensions.Logging;
using Polly;

namespace NotificationService.Infraestructure.Extensions
{
    public static class PollyContextExtensions
    {
        public static ILogger? GetLogger(this Context context)
        {
            if (context.TryGetValue("logger", out var logger))
            {
                return logger as ILogger;
            }
            return null;
        }
    }
}
