using NotificationService.Application.DTOs;
using RabbitMQ.Client;
using System.Text.Json;
using Testcontainers.RabbitMq;

namespace NotificationService.UnitTests
{
    public class RabbitMqNotificationListenerTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
         .WithImage("rabbitmq:3-management")
         .Build();
        private IConnection _connection;

        public async Task InitializeAsync()
        {
            await _rabbitMqContainer.StartAsync();
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_rabbitMqContainer.GetConnectionString())
            };
            _connection = factory.CreateConnection();
        }

        [Fact]
        public async Task Should_Process_Valid_Message()
        {
            // Arrange
            var channel = _connection.CreateModel();
            var message = new NotificationMessageDto
            {
                Email = "test@test.com",
                Subject = "Test",
                Body = "Content"
            };

            // Act
            channel.BasicPublish("", "notification.events", null,
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Assert
            // Implementar verificação do processamento
        }

        public async Task DisposeAsync() => await _rabbitMqContainer.DisposeAsync();
    }
}
