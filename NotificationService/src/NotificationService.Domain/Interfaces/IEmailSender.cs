using NotificationService.Domain.DTOs;

namespace NotificationService.Domain.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(NotificationMessageDto dto);
    }
}
