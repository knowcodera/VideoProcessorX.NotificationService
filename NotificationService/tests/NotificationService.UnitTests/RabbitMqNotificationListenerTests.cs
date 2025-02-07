using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
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
        public async Task DeveProcessarMensagemComSucesso()
        {
            // Arrange
            var message = new NotificationMessageDto
            {
                Email = "teste@teste.com",
                Subject = "Teste",
                Body = "Corpo da mensagem"
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(messageBody),
                DeliveryTag = 1
            };

            _emailSenderMock.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _listener.ProcessMessageAsync(null, eventArgs);

            // Assert
            _emailSenderMock.Verify(s => s.SendEmailAsync(message.Email, message.Subject, message.Body), Times.Once);
            _channelMock.Verify(c => c.BasicAck(eventArgs.DeliveryTag, false), Times.Once);
        }

        [Fact]
        public async Task DeveRejeitarMensagemInvalida()
        {
            // Arrange
            var invalidMessage = Encoding.UTF8.GetBytes("Mensagem inválida");
            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(invalidMessage),
                DeliveryTag = 1
            };

            // Act
            await _listener.ProcessMessageAsync(null, eventArgs);

            // Assert
            _channelMock.Verify(c => c.BasicNack(eventArgs.DeliveryTag, false, false), Times.Once);
        }

        [Fact]
        public async Task DeveTentarNovamente_SeEmailFalhar()
        {
            // Arrange
            var message = new NotificationMessageDto
            {
                Email = "teste@teste.com",
                Subject = "Teste",
                Body = "Corpo da mensagem"
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(messageBody),
                DeliveryTag = 1
            };

            _emailSenderMock.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("Falha no envio"));

            // Act
            await _listener.ProcessMessageAsync(null, eventArgs);

            // Assert
            _channelMock.Verify(c => c.BasicNack(eventArgs.DeliveryTag, false, true), Times.Once);
        }

        [Fact]
        public void DeveFecharConexoes_AoFinalizar()
        {
            // Act
            _listener.Dispose();

            // Assert
            _channelMock.Verify(c => c.Close(), Times.Once);
            _connectionMock.Verify(c => c.Close(), Times.Once);
        }
    }
}
