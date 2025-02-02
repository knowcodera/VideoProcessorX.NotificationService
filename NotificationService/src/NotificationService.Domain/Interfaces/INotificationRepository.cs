using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task<Notification> GetByIdAsync(int id);
    }
}
