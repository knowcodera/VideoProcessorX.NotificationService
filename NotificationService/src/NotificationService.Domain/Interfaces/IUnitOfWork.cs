namespace NotificationService.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        INotificationRepository Notifications { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task SaveChangesAsync();
    }
}
