using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Services
{
    public class NotificationEmailService : INotificationEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationRepository _notificationRepo; // opcional

        public NotificationEmailService(IEmailSender emailSender,
                                   INotificationRepository notificationRepo)
        {
            _emailSender = emailSender;
            _notificationRepo = notificationRepo;
        }

        public async Task NotifyAsync(string email, string subject, string body)
        {
            // (Opcional) Cria entidade e salva no repositório antes de enviar
            var notification = new Notification
            {
                Email = email,
                Subject = subject,
                Body = body
                // Sent = false, SentAt = null...
            };
            await _notificationRepo.CreateAsync(notification);

            // Envia o e-mail (via MailKit, etc.)
            await _emailSender.SendEmailAsync(email, subject, body);

            // (Opcional) marca como enviado
            notification.Sent = true;
            notification.SentAt = DateTime.UtcNow;
            await _notificationRepo.UpdateAsync(notification);
        }
    }
}
