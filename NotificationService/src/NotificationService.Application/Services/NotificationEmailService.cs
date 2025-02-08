using Microsoft.Extensions.Logging;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace NotificationService.Application.Services
{
    public class NotificationEmailService : INotificationEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationEmailService> _logger;

        public NotificationEmailService(
            IEmailSender emailSender,
            IUnitOfWork unitOfWork,
            ILogger<NotificationEmailService> logger)
        {
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task NotifyAsync(string email, string subject, string body)
        {
            var notification = new Notification
            {
                Email = email,
                Subject = subject,
                Body = body,
                CreatedAt = DateTime.UtcNow,
                Sent = false,
                Attempts = 0
            };

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Registra a tentativa
                notification.Attempts++;
                await _unitOfWork.Notifications.CreateAsync(notification);

                var dto = JsonSerializer.Deserialize<NotificationMessageDto>(body);

                // Tenta enviar o e-mail
                await _emailSender.SendEmailAsync(dto);

                // Atualiza status
                notification.Sent = true;
                notification.SentAt = DateTime.UtcNow;
                await _unitOfWork.Notifications.UpdateAsync(notification);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();

                // Registra a falha
                notification.Attempts++;
                notification.LastError = ex.Message;
                await _unitOfWork.Notifications.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogError(ex, "Falha ao enviar notificação para {Email}", email);
                throw;
            }
        }
    }
}
