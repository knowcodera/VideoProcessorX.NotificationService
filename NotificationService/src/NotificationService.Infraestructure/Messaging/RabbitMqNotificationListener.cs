using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infraestructure.Messaging
{
    public class RabbitMqNotificationListener : BackgroundService
    {
        private readonly ILogger<RabbitMqNotificationListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection _connection;
        private IModel _channel;
        private const string QueueName = "notification.events";
        private const string DeadLetterExchange = "dlx.notifications";
        private readonly IConfiguration _configuration;

        public RabbitMqNotificationListener(
            ILogger<RabbitMqNotificationListener> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
             _configuration = configuration;
            InitializeRabbitMq();
           
        }

        private void InitializeRabbitMq()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"],
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(15)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Fanout, durable: true);
                var dlxQueue = _channel.QueueDeclare("dead_letter_queue", durable: true, exclusive: false, autoDelete: false);

                _channel.QueueBind(dlxQueue.QueueName, DeadLetterExchange, "");

                var args = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", DeadLetterExchange }
                };

                _channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: args);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("RabbitMQ listener initialized for queue: {QueueName}", QueueName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "RabbitMQ initialization failed");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                _logger.LogInformation("Stopping queue consumption: {QueueName}", QueueName));

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += ProcessMessageAsync;

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        public async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _scopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<RabbitMqNotificationListener>>();

            var messageJson = string.Empty;
            try
            {
                var body = ea.Body.ToArray();
                messageJson = Encoding.UTF8.GetString(body);

                logger.LogDebug("Processing message: {DeliveryTag}", ea.DeliveryTag);

                var dto = JsonSerializer.Deserialize<NotificationMessageDto>(messageJson);

                if (!IsValidMessage(dto))
                {
                    logger.LogWarning("Invalid message received: {Content}", messageJson);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await emailSender.SendEmailAsync(dto);

                _channel.BasicAck(ea.DeliveryTag, false);
                logger.LogInformation("Email sent successfully to {Email}", dto.Email);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Message deserialization failed: {Content}", messageJson);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message: {Content}", messageJson);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        }

        private bool IsValidMessage(NotificationMessageDto message)
        {
            return !string.IsNullOrWhiteSpace(message?.Email) &&
                   !string.IsNullOrWhiteSpace(message.Subject) &&
                   !string.IsNullOrWhiteSpace(message.Body);
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up RabbitMQ resources");
            }
            finally
            {
                _channel?.Dispose();
                _connection?.Dispose();
                base.Dispose();
            }
        }
    }
}
