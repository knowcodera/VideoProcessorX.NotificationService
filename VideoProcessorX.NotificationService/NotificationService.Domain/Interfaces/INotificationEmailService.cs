namespace NotificationService.Domain.Interfaces
{
    public interface INotificationEmailService
    {
        Task NotifyAsync(string email, string subject, string body);
    }
}
