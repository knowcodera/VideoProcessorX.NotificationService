using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.Services;
using NotificationService.Domain.DTOs;
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
            public async Task NotifyAsync_ShouldHandleRetriesAndFailure()
            {
                // Arrange
                var service = new NotificationEmailService(
                    _emailSenderMock.Object,
                    _unitOfWorkMock.Object,
                    _loggerMock.Object);

                var dto = new NotificationMessageDto
                {
                    Email = "test@test.com",
                    Subject = "Test",
                    Body = "Test Body"
                };

                _emailSenderMock.Setup(s => s.SendEmailAsync(It.IsAny<NotificationMessageDto>()))
                    .ThrowsAsync(new Exception("SMTP Error"));

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => service.NotifyAsync(dto.Email, dto.Subject, dto.Body));

                _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
                _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Notification>(n =>
                    n.Sent == false &&
                    n.Attempts == 1 &&
                    !string.IsNullOrEmpty(n.LastError))), Times.Once);
            }

            [Fact]
            public async Task NotifyAsync_ShouldHandleAttachmentDownloadFailure()
            {
                // Arrange
                var fileStorageMock = new Mock<IFileStorageService>();
                fileStorageMock.Setup(f => f.GetFileStreamAsync(It.IsAny<string>()))
                    .ReturnsAsync((Stream)null);

                var service = new NotificationEmailService(
                    _emailSenderMock.Object,
                    _unitOfWorkMock.Object,
                    _loggerMock.Object);

                var dto = new NotificationMessageDto
                {
                    Email = "test@test.com",
                    Subject = "Test",
                    Body = "Test Body",
                    AttachmentPath = "invalid/path.zip"
                };

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => service.NotifyAsync(dto.Email, dto.Subject, dto.Body));

                _loggerMock.Verify(log => log.LogWarning(
                    "Não foi possível obter o stream do blob: {Path}",
                    dto.AttachmentPath), Times.Once);
            }
        }
    }
}
