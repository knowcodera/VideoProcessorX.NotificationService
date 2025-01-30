using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<int> CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        // etc...
    }
}
