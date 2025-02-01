using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infraestructure.Persistence
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _dbContext;

        public NotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAsync(Notification notification)
        {
            await _dbContext.Notifications.AddAsync(notification);
        }

        public async Task UpdateAsync(Notification notification)
        {
            _dbContext.Notifications.Update(notification);
        }

        public async Task<Notification> GetByIdAsync(int id)
        {
            return await _dbContext.Notifications.FindAsync(id);
        }
    }
}
