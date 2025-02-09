using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Domain.DTOs;
using NotificationService.Infraestructure.Email;
using Polly.CircuitBreaker;
using System.Net.Mail;

namespace NotificationService.UnitTests
{
    public class FluentEmailSenderTests
    {
        [Fact]
        public async Task SendEmailAsync_DeveEnviarComSucesso()
        {
            // Arrange
            var fluentEmailMock = new Mock<IFluentEmail>();
            var loggerMock = new Mock<ILogger<FluentEmailSender>>();

            fluentEmailMock.SetupSequence(f => f.SendAsync(null))
                .ThrowsAsync(new SmtpException("Temporary failure"))
                .ThrowsAsync(new SmtpException("Temporary failure"))
                .ReturnsAsync(new SendResponse { MessageId = "123" });

            var sender = new FluentEmailSender(fluentEmailMock.Object, loggerMock.Object);

            var dto = new NotificationMessageDto
            {
                Email = "teste@teste.com",
                Subject = "assunto",
                Body = "corpo",
                AttachmentPath = null,
                IsProcessingUpdate = false
            };

            // Act
            await sender.SendEmailAsync(dto);

            // Assert
            fluentEmailMock.Verify(f => f.SendAsync(null), Times.Exactly(3));
        }

        [Fact]
        public async Task SendEmailAsync_QuandoCircuitoAberto_DeveLancarExcecao()
        {
            // Arrange
            var fluentEmailMock = new Mock<IFluentEmail>();
            var loggerMock = new Mock<ILogger<FluentEmailSender>>();

            var dto = new NotificationMessageDto
            {
                Email = "teste@teste.com",
                Subject = "assunto",
                Body = "corpo",
                AttachmentPath = null,
                IsProcessingUpdate = false
            };

            fluentEmailMock.Setup(f => f.SendAsync(null))
                .ThrowsAsync(new SmtpException("Server busy"));

            var sender = new FluentEmailSender(fluentEmailMock.Object, loggerMock.Object);

            // Act & Assert
            for (int i = 0; i < 5; i++)
            {
                await Assert.ThrowsAsync<SmtpException>(() =>
                    sender.SendEmailAsync(dto));
            }

            await Assert.ThrowsAsync<BrokenCircuitException>(() =>
                sender.SendEmailAsync(dto));
        }
    }
}

