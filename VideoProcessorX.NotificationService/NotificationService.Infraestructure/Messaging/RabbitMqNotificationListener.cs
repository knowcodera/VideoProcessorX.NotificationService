using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
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
        private const string QUEUE_NAME = "notification.events";

        public RabbitMqNotificationListener(
            ILogger<RabbitMqNotificationListener> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1) Criar conexão com RabbitMQ
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: QUEUE_NAME,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // 2) Configura consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(
                queue: QUEUE_NAME,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Consumindo fila: {QueueName}", QUEUE_NAME);

            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                // 1) Cria escopo a cada mensagem
                using var scope = _scopeFactory.CreateScope();

                // 2) Resolve IEmailSender (Scoped) dentro do escopo
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                // Se houver outro repositório ou DbContext, também resolva aqui

                // 3) Processa a mensagem
                var body = e.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Mensagem recebida: {msg}", messageJson);

                // Exemplo: sendEmail ou chamar NotificationService etc.
                // await emailSender.SendEmailAsync("...", "...", "...");

                _channel.BasicAck(e.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem.");
                _channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
