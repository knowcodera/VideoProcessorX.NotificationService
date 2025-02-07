using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.UnitTests
{
    namespace NotificationService.UnitTests
    {
        public class NotificationEmailServiceTests
        {
            private readonly Mock<IEmailSender> _emailSenderMock;
            private readonly Mock<IUnitOfWork> _unitOfWorkMock;
            private readonly Mock<INotificationRepository> _notificationRepositoryMock;
            private readonly Mock<ILogger<NotificationEmailService>> _loggerMock;

            public NotificationEmailServiceTests()
            {
                _emailSenderMock = new Mock<IEmailSender>();
                _unitOfWorkMock = new Mock<IUnitOfWork>();
                _notificationRepositoryMock = new Mock<INotificationRepository>();
                _loggerMock = new Mock<ILogger<NotificationEmailService>>();

                _unitOfWorkMock
                    .Setup(u => u.Notifications)
                    .Returns(_notificationRepositoryMock.Object);
            }

            [Fact]
            public async Task NotifyAsync_DeveEnviarEmailComSucesso_MarcarNotificacaoComoEnviada()
            {
                // Arrange
                var service = new NotificationEmailService(
                    _emailSenderMock.Object,
                    _unitOfWorkMock.Object,
                    _loggerMock.Object
                );

                var email = "teste@teste.com";
                var subject = "Assunto de Teste";
                var body = "Corpo de Teste";

                // Act
                await service.NotifyAsync(email, subject, body);

                // Assert
                _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
                _notificationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
                _emailSenderMock.Verify(s => s.SendEmailAsync(email, subject, body), Times.Once);
                _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Sent == true)), Times.Once);
                _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            }

            [Fact]
            public async Task NotifyAsync_QuandoFalhaNoEnvio_EmailNaoMarcadoComoEnviado_FazRollback()
            {
                // Arrange
                var service = new NotificationEmailService(
                    _emailSenderMock.Object,
                    _unitOfWorkMock.Object,
                    _loggerMock.Object
                );

                var email = "teste@teste.com";
                var subject = "Assunto de Teste";
                var body = "Corpo de Teste";

                _emailSenderMock
                    .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new Exception("Falha no servidor SMTP"));

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => service.NotifyAsync(email, subject, body));

                _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
                _notificationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
                _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
                _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Sent == false && n.Attempts == 2)), Times.Once);
                _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
            }
        }
    }
}
