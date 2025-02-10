using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Interfaces;
using NotificationService.Infraestructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.UnitTests
{
    public class RabbitMqNotificationListenerTests
    {
        private readonly Mock<IConnection> _connectionMock;
        private readonly Mock<IModel> _channelMock;
        private readonly Mock<ILogger<RabbitMqNotificationListener>> _loggerMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IEmailSender> _emailSenderMock;

        private readonly RabbitMqNotificationListener _listener;

        public RabbitMqNotificationListenerTests()
        {
            _connectionMock = new Mock<IConnection>();
            _channelMock = new Mock<IModel>();
            _loggerMock = new Mock<ILogger<RabbitMqNotificationListener>>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _emailSenderMock = new Mock<IEmailSender>();

            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IEmailSender))).Returns(_emailSenderMock.Object);

            _connectionMock.Setup(conn => conn.CreateModel()).Returns(_channelMock.Object);

        }


        [Fact]
        public async Task ProcessMessageAsync_ShouldHandleInvalidMessage()
        {
            // Arrange
            var invalidMessage = "invalid json";
            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(invalidMessage)),
                DeliveryTag = 1
            };

            // Act
            await _listener.ProcessMessageAsync(null, eventArgs);

            // Assert
            _channelMock.Verify(c => c.BasicNack(eventArgs.DeliveryTag, false, false), Times.Once);
            _loggerMock.Verify(log => log.LogError(It.IsAny<Exception>(), "Message deserialization failed"), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_ShouldHandleEmailSendFailure()
        {
            // Arrange
            var message = new NotificationMessageDto
            {
                Email = "test@test.com",
                Subject = "Test",
                Body = "Test Body"
            };

            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(message)),
                DeliveryTag = 1
            };

            _emailSenderMock.Setup(s => s.SendEmailAsync(It.IsAny<NotificationMessageDto>()))
                .ThrowsAsync(new Exception("SMTP Error"));

            // Act
            await _listener.ProcessMessageAsync(null, eventArgs);

            // Assert
            _channelMock.Verify(c => c.BasicNack(eventArgs.DeliveryTag, false, true), Times.Once);
            _loggerMock.Verify(log => log.LogError(It.IsAny<Exception>(), "Error processing message"), Times.Once);
        }
    }
}

