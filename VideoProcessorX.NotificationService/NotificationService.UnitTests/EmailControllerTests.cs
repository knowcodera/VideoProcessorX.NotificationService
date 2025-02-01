using Microsoft.AspNetCore.Mvc;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;
using NotificationService.Presentation.Controllers;

namespace NotificationService.UnitTests
{
    public class EmailControllerTests
    {
        [Fact]
        public async Task SendTestEmail_DeveRetornarOk_QuandoNotificacaoEnviada()
        {
            // Arrange
            var notificationServiceMock = new Mock<INotificationEmailService>();

            var controller = new EmailController(notificationServiceMock.Object);

            var requestDto = new EmailRequestDto
            {
                To = "teste@teste.com",
                Subject = "Teste Subject",
                Body = "Teste Body"
            };

            // Act
            var result = await controller.SendTestEmail(requestDto);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            notificationServiceMock.Verify(
                s => s.NotifyAsync(requestDto.To, requestDto.Subject, requestDto.Body),
                Times.Once);
        }
    }
}
